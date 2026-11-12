using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Runtime.Security;
using Abp.Runtime.Validation;
using Abp.Timing;
using Abp.UI.Inputs;
using StockPilot.Authorization.Users;
using StockPilot.Entity;
using StockPilot.Features;
using StockPilot.MultiTenancy.Dto;

namespace StockPilot.MultiTenancy
{
	[AbpAuthorize(new string[] { "Pages.Tenants" })]
	public class TenantAppService : StockPilotAppServiceBase, ITenantAppService, IApplicationService, ITransientDependency
	{
		private readonly IRepository<User, long> _userRepository;

		private readonly IRepository<TenantFeatureSetting, long> _tenantFeatureRepository;

		private readonly IRepository<ItemMaster> _itemRepository;

		public StoreFeatureManager StoreFeatureManager { get; set; }

		public TenantAppService(IRepository<User, long> userRepository, IRepository<TenantFeatureSetting, long> tenantFeatureRepository, IRepository<ItemMaster> itemRepository)
		{
			_userRepository = userRepository;
			_tenantFeatureRepository = tenantFeatureRepository;
			_itemRepository = itemRepository;
		}

		public async Task<PagedResultDto<TenantListDto>> GetTenants(GetTenantsInput input)
		{
			DateTime today = Clock.Now.Date;
			int num = await WarnBeforeDaysAsync();
			DateTime warnLimit = today.AddDays(num);
			IQueryable<int> withOverrides = from f in _tenantFeatureRepository.GetAll()
				select f.TenantId;
			IQueryable<Tenant> query = base.TenantManager.Tenants.Include((Tenant t) => t.Edition).WhereIf(input.IsActive.HasValue, (Tenant t) => t.IsActive == input.IsActive.Value).WhereIf(input.EditionId.HasValue && input.EditionId.Value > 0, (Tenant t) => t.EditionId == (int?)input.EditionId.Value)
				.WhereIf(input.EditionId.HasValue && input.EditionId.Value == 0, (Tenant t) => t.EditionId == (int?)null)
				.WhereIf(input.HasCustomFeatures == true, (Tenant t) => withOverrides.Contains(t.Id))
				.WhereIf(input.HasCustomFeatures == false, (Tenant t) => !withOverrides.Contains(t.Id));
			if (!input.Filter.IsNullOrWhiteSpace())
			{
				string filter = input.Filter.Trim();
				query = query.Where((Tenant t) => t.Name.Contains(filter) || t.TenancyName.Contains(filter) || t.ContactName.Contains(filter) || t.ContactPhone.Contains(filter) || t.ContactEmail.Contains(filter));
			}
			switch (input.SubscriptionStatus)
			{
			case TenantSubscriptionStatus.None:
				query = query.Where((Tenant t) => t.SubscriptionEndDate == null);
				break;
			case TenantSubscriptionStatus.Expired:
				query = query.Where((Tenant t) => DbFunctions.TruncateTime(t.SubscriptionEndDate) < today);
				break;
			case TenantSubscriptionStatus.ExpiringSoon:
				query = query.Where((Tenant t) => DbFunctions.TruncateTime(t.SubscriptionEndDate) >= today && DbFunctions.TruncateTime(t.SubscriptionEndDate) <= warnLimit);
				break;
			case TenantSubscriptionStatus.Active:
				query = query.Where((Tenant t) => DbFunctions.TruncateTime(t.SubscriptionEndDate) > warnLimit);
				break;
			}
			int tenantCount = await query.CountAsync();
			List<TenantListDto> rows = (await query.OrderBy(input.Sorting).PageBy(input).ToListAsync()).MapTo<List<TenantListDto>>();
			await FillUsageAsync(rows.Select((TenantListDto r) => r.Id).ToList(), delegate(int id, int users, int overrides)
			{
				TenantListDto tenantListDto = rows.Single((TenantListDto r) => r.Id == id);
				tenantListDto.UserCount = users;
				tenantListDto.FeatureOverrideCount = overrides;
			});
			foreach (TenantListDto item in rows)
			{
				item.SubscriptionStatus = TenantSubscription.GetStatus(item.SubscriptionEndDate, today, (warnLimit - today).Days);
				item.IsSubscriptionExpired = TenantSubscription.IsExpired(item.SubscriptionEndDate, today);
			}
			return new PagedResultDto<TenantListDto>(tenantCount, rows);
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Create" })]
		[UnitOfWork(IsDisabled = true)]
		public async Task CreateTenant(CreateTenantInput input)
		{
			int tenantId = await base.TenantManager.CreateWithAdminUserAsync(input.TenancyName, input.Name, input.AdminPassword, input.AdminEmailAddress, input.ConnectionString, input.IsActive, input.EditionId, input.ShouldChangePasswordOnNextLogin, input.SendActivationEmail, input.AdminPhoneNumber, input.AdminName, input.AdminSurname);
			using (IUnitOfWorkCompleteHandle uow = base.UnitOfWorkManager.Begin())
			{
				Tenant obj = await base.TenantManager.GetByIdAsync(tenantId);
				obj.SubscriptionEndDate = input.SubscriptionEndDate?.Date;
				obj.ContactName = Clean(input.ContactName);
				obj.ContactEmail = Clean(input.ContactEmail);
				obj.ContactPhone = Clean(input.ContactPhone);
				obj.Address = Clean(input.Address);
				obj.City = Clean(input.City);
				obj.CountryName = Clean(input.CountryName);
				obj.BusinessType = Clean(input.BusinessType);
				obj.Notes = Clean(input.Notes);
				await uow.CompleteAsync();
			}
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Edit" })]
		public async Task<TenantEditDto> GetTenantForEdit(EntityDto input)
		{
			Tenant tenant = await base.TenantManager.Tenants.Include((Tenant t) => t.Edition).FirstOrDefaultAsync((Tenant t) => t.Id == input.Id);
			if ((object)tenant == null)
			{
				tenant = await base.TenantManager.GetByIdAsync(input.Id);
			}
			Tenant tenant2 = tenant;
			TenantEditDto tenantEditDto = tenant2.MapTo<TenantEditDto>();
			tenantEditDto.ConnectionString = SimpleStringCipher.Instance.Decrypt(tenantEditDto.ConnectionString);
			tenantEditDto.EditionDisplayName = tenant2.Edition?.DisplayName;
			DateTime today = Clock.Now.Date;
			TenantEditDto tenantEditDto2 = tenantEditDto;
			DateTime? subscriptionEndDate = tenant2.SubscriptionEndDate;
			DateTime today2 = today;
			tenantEditDto2.SubscriptionStatus = TenantSubscription.GetStatus(subscriptionEndDate, today2, await WarnBeforeDaysAsync());
			tenantEditDto.IsSubscriptionExpired = TenantSubscription.IsExpired(tenant2.SubscriptionEndDate, today);
			await FillUsageAsync(new List<int> { tenant2.Id }, delegate(int id, int users, int overrides)
			{
				tenantEditDto.UserCount = users;
				tenantEditDto.FeatureOverrideCount = overrides;
			});
			return tenantEditDto;
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Edit" })]
		public async Task UpdateTenant(TenantEditDto input)
		{
			Tenant tenant = await base.TenantManager.GetByIdAsync(input.Id);
			tenant.TenancyName = input.TenancyName.Trim();
			tenant.Name = input.Name.Trim();
			tenant.ConnectionString = (input.ConnectionString.IsNullOrWhiteSpace() ? null : SimpleStringCipher.Instance.Encrypt(input.ConnectionString));
			tenant.EditionId = input.EditionId;
			tenant.IsActive = input.IsActive;
			tenant.SubscriptionEndDate = input.SubscriptionEndDate?.Date;
			tenant.ContactName = Clean(input.ContactName);
			tenant.ContactEmail = Clean(input.ContactEmail);
			tenant.ContactPhone = Clean(input.ContactPhone);
			tenant.Address = Clean(input.Address);
			tenant.City = Clean(input.City);
			tenant.CountryName = Clean(input.CountryName);
			tenant.BusinessType = Clean(input.BusinessType);
			tenant.Notes = Clean(input.Notes);
			await base.TenantManager.UpdateAsync(tenant);
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Delete" })]
		public async Task DeleteTenant(EntityDto input)
		{
			Tenant tenant = await base.TenantManager.GetByIdAsync(input.Id);
			await base.TenantManager.DeleteAsync(tenant);
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Edit" })]
		public async Task SetActive(SetTenantActiveInput input)
		{
			Tenant tenant = await base.TenantManager.GetByIdAsync(input.Id);
			tenant.IsActive = input.IsActive;
			await base.TenantManager.UpdateAsync(tenant);
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Edit" })]
		public async Task ExtendSubscription(ExtendSubscriptionInput input)
		{
			Tenant tenant = await base.TenantManager.GetByIdAsync(input.Id);
			tenant.SubscriptionEndDate = input.NewEndDate.Date;
			await base.TenantManager.UpdateAsync(tenant);
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.ChangeFeatures" })]
		public async Task<TenantFeaturesForEditOutput> GetTenantFeaturesForEdit(EntityDto input)
		{
			Tenant tenant = await base.TenantManager.Tenants.Include((Tenant t) => t.Edition).FirstOrDefaultAsync((Tenant t) => t.Id == input.Id);
			if ((object)tenant == null)
			{
				tenant = await base.TenantManager.GetByIdAsync(input.Id);
			}
			Tenant tenant2 = tenant;
			Dictionary<string, string> overrides = await StoreFeatureManager.GetOverridesAsync(tenant2.Id);
			TenantFeaturesForEditOutput output = new TenantFeaturesForEditOutput
			{
				TenantId = tenant2.Id,
				EditionId = tenant2.EditionId,
				EditionDisplayName = tenant2.Edition?.DisplayName
			};
			Dictionary<string, string> effective = new Dictionary<string, string>();
			foreach (Feature feature in StoreFeatureManager.GetAllFeatures())
			{
				string editionValue = await StoreFeatureManager.GetEditionValueAsync(tenant2.EditionId, feature);
				overrides.TryGetValue(feature.Name, out var tenantValue);
				effective[feature.Name] = tenantValue ?? editionValue;
				NumericValueValidator validator = feature.InputType?.Validator as NumericValueValidator;
				int depth = 0;
				for (Feature parent = feature.Parent; parent != null; parent = parent.Parent)
				{
					depth++;
				}
				output.Features.Add(new TenantFeatureEditDto
				{
					Name = feature.Name,
					DisplayName = ((feature.DisplayName == null) ? feature.Name : feature.DisplayName.Localize(new LocalizationContext(base.LocalizationManager))),
					Description = feature.Description?.Localize(new LocalizationContext(base.LocalizationManager)),
					ParentName = feature.Parent?.Name,
					Depth = depth,
					InputType = ((feature.InputType is CheckboxInputType) ? "checkbox" : ((validator != null) ? "number" : "text")),
					MinValue = validator?.MinValue,
					MaxValue = validator?.MaxValue,
					EditionValue = editionValue,
					TenantValue = tenantValue,
					IsOverridden = (tenantValue != null)
				});
			}
			StoreFeatureManager.ApplyParentRule(effective);
			foreach (TenantFeatureEditDto row in output.Features)
			{
				row.EffectiveValue = effective[row.Name];
			}
			return output;
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.ChangeFeatures" })]
		[Audited]
		public async Task UpdateTenantFeatures(UpdateTenantFeaturesInput input)
		{
			await StoreFeatureManager.SetTenantFeaturesAsync(input.Id, input.FeatureValues.Select((NameValueDto fv) => new NameValue(fv.Name, fv.Value)));
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.ChangeFeatures" })]
		[Audited]
		public async Task ResetTenantSpecificFeatures(EntityDto input)
		{
			await StoreFeatureManager.ResetTenantFeaturesAsync(input.Id);
		}

		public async Task<TenantFeatureUsageDto> GetFeatureUsage(EntityDto input)
		{
			Tenant tenant = await base.TenantManager.GetByIdAsync(input.Id);
			using (base.CurrentUnitOfWork.SetTenantId(tenant.Id))
			{
				TenantFeatureUsageDto tenantFeatureUsageDto = new TenantFeatureUsageDto();
				TenantFeatureUsageDto tenantFeatureUsageDto2 = tenantFeatureUsageDto;
				tenantFeatureUsageDto2.UserCount = await _userRepository.CountAsync();
				TenantFeatureUsageDto tenantFeatureUsageDto3 = tenantFeatureUsageDto;
				tenantFeatureUsageDto3.ActiveItemCount = await _itemRepository.CountAsync((ItemMaster i) => i.IsActive);
				TenantFeatureUsageDto tenantFeatureUsageDto4 = tenantFeatureUsageDto;
				tenantFeatureUsageDto4.MaxUserCount = (await base.FeatureChecker.GetValueAsync(tenant.Id, "App.MaxUserCount")).To<int>();
				TenantFeatureUsageDto tenantFeatureUsageDto5 = tenantFeatureUsageDto;
				tenantFeatureUsageDto5.MaxItemCount = (await base.FeatureChecker.GetValueAsync(tenant.Id, "App.MaxItemCount")).To<int>();
				return tenantFeatureUsageDto;
			}
		}

		public async Task UnlockTenantAdmin(EntityDto input)
		{
			using (base.CurrentUnitOfWork.SetTenantId(input.Id))
			{
				User tenantAdmin = await base.UserManager.FindByNameAsync("admin");
				if (tenantAdmin != null)
				{
					tenantAdmin.Unlock();
				}
			}
		}

		private async Task FillUsageAsync(List<int> tenantIds, Action<int, int, int> fill)
		{
			if (tenantIds.Count == 0)
			{
				return;
			}
			using (base.CurrentUnitOfWork.DisableFilter("MayHaveTenant", "MustHaveTenant"))
			{
				var users = await (from u in _userRepository.GetAll()
					where u.TenantId != (int?)null && tenantIds.Contains(u.TenantId.Value)
					group u by u.TenantId.Value into g
					select new
					{
						TenantId = g.Key,
						Count = g.Count()
					}).ToListAsync();
				var overrides = await (from f in _tenantFeatureRepository.GetAll()
					where tenantIds.Contains(f.TenantId)
					group f by f.TenantId into g
					select new
					{
						TenantId = g.Key,
						Count = g.Count()
					}).ToListAsync();
				foreach (int id in tenantIds)
				{
					fill(id, (from u in users
						where u.TenantId == id
						select u.Count).FirstOrDefault(), (from o in overrides
						where o.TenantId == id
						select o.Count).FirstOrDefault());
				}
			}
		}

		private async Task<int> WarnBeforeDaysAsync()
		{
			return await base.SettingManager.GetSettingValueForApplicationAsync<int>("App.Subscription.WarnBeforeDays");
		}

		private static string Clean(string value)
		{
			if (!value.IsNullOrWhiteSpace())
			{
				return value.Trim();
			}
			return null;
		}
	}
}
