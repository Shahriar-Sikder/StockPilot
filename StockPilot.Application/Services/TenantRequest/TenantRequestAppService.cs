using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using Abp.Timing;
using Abp.UI;
using StockPilot.Authorization.Users;
using StockPilot.Editions;
using StockPilot.Features;
using StockPilot.MultiTenancy;
using StockPilot.Notifications;
using StockPilot.Services.Common;
using StockPilot.Services.TenantRequest.Dtos;

namespace StockPilot.Services.TenantRequest
{
	[AbpAuthorize(new string[] { "Pages.Tenants.Requests" })]
	public class TenantRequestAppService : StockPilotAppServiceBase, ITenantRequestAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "requestNo", "RequestNo" },
			{ "storeName", "StoreName" },
			{ "tenancyName", "TenancyName" },
			{ "contactName", "ContactName" },
			{ "contactPhone", "ContactPhone" },
			{ "city", "City" },
			{ "adminEmailAddress", "AdminEmailAddress" },
			{ "status", "Status" },
			{ "reviewedTime", "ReviewedTime" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.MultiTenancy.TenantRequest> _requestRepository;

		private readonly TenantRequestManager _requestManager;

		private readonly TenantRequestEmailer _emailer;

		private readonly IAppNotifier _appNotifier;

		private readonly StoreFeatureManager _storeFeatureManager;

		private readonly ITenantDemoDataSeeder _demoDataSeeder;

		private readonly EditionManager _editionManager;

		public TenantRequestAppService(IRepository<StockPilot.MultiTenancy.TenantRequest> requestRepository, TenantRequestManager requestManager, TenantRequestEmailer emailer, IAppNotifier appNotifier, StoreFeatureManager storeFeatureManager, ITenantDemoDataSeeder demoDataSeeder, EditionManager editionManager)
		{
			_editionManager = editionManager;
			_requestRepository = requestRepository;
			_requestManager = requestManager;
			_emailer = emailer;
			_appNotifier = appNotifier;
			_storeFeatureManager = storeFeatureManager;
			_demoDataSeeder = demoDataSeeder;
		}

		public async Task<PagedResultDto<TenantRequestListDto>> GetAll(TenantRequestGridInput input)
		{
			UseHostSide();
			IQueryable<StockPilot.MultiTenancy.TenantRequest> query = _requestRepository.GetAll().WhereIf(input.Status.HasValue, (StockPilot.MultiTenancy.TenantRequest r) => (int)r.Status == (int)input.Status.Value).WhereIf(input.DateFrom.HasValue, (StockPilot.MultiTenancy.TenantRequest r) => r.CreationTime >= input.DateFrom.Value)
				.WhereIf(input.DateTo.HasValue, (StockPilot.MultiTenancy.TenantRequest r) => r.CreationTime < DbFunctions.AddDays(input.DateTo.Value, 1));
			if (!input.Filter.IsNullOrWhiteSpace())
			{
				string filter = input.Filter.Trim();
				query = query.Where((StockPilot.MultiTenancy.TenantRequest r) => r.RequestNo.Contains(filter) || r.StoreName.Contains(filter) || r.TenancyName.Contains(filter) || r.ContactName.Contains(filter) || r.ContactPhone.Contains(filter) || r.AdminEmailAddress.Contains(filter) || r.ContactEmail.Contains(filter));
			}
			int totalCount = await query.CountAsync();
			List<StockPilot.MultiTenancy.TenantRequest> rows = await query.OrderBy(MasterDataAppServiceBase.TranslateSorting(input.Sorting, "CreationTime descending", SortFields)).PageBy(input).ToListAsync();
			return new PagedResultDto<TenantRequestListDto>(totalCount, base.ObjectMapper.Map<List<TenantRequestListDto>>(rows));
		}

		public async Task<TenantRequestCountsDto> GetCounts()
		{
			UseHostSide();
			var counts = await (from r in _requestRepository.GetAll()
				group r by r.Status into g
				select new
				{
					Status = g.Key,
					Count = g.Count()
				}).ToListAsync();
			return new TenantRequestCountsDto
			{
				Pending = Of(TenantRequestStatus.Pending),
				Approved = Of(TenantRequestStatus.Approved),
				Rejected = Of(TenantRequestStatus.Rejected),
				Cancelled = Of(TenantRequestStatus.Cancelled)
			};
			int Of(TenantRequestStatus status)
			{
				return (from c in counts
					where c.Status == status
					select c.Count).FirstOrDefault();
			}
		}

		public async Task<EditTenantRequestDto> GetForEdit(EntityDto input)
		{
			UseHostSide();
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditTenantRequestDto>(await GetRequestAsync(input.Id));
		}

		public async Task Update(EditTenantRequestDto input)
		{
			UseHostSide();
			StockPilot.MultiTenancy.TenantRequest request = await GetRequestAsync(input.Id);
			CheckPending(request);
			string tenancyName = input.TenancyName.Trim();
			await _requestManager.CheckTenancyNameIsFreeAsync(tenancyName, request.Id);
			request.TenancyName = tenancyName;
			request.StoreName = input.StoreName.Trim();
			request.ContactName = Clean(input.ContactName);
			request.ContactEmail = Clean(input.ContactEmail);
			request.ContactPhone = Clean(input.ContactPhone);
			request.Address = Clean(input.Address);
			request.City = Clean(input.City);
			request.CountryName = Clean(input.CountryName);
			request.BusinessType = Clean(input.BusinessType);
			request.Notes = Clean(input.Notes);
			request.AdminName = input.AdminName.Trim();
			request.AdminSurname = input.AdminSurname.Trim();
			request.AdminEmailAddress = input.AdminEmailAddress.Trim();
			request.AdminPhoneNumber = User.NormalizePhoneNumber(input.AdminPhoneNumber);
			request.RequestedEditionId = input.RequestedEditionId;
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Requests.Approve" })]
		public async Task Approve(ApproveTenantRequestDto input)
		{
			UseHostSide();
			StockPilot.MultiTenancy.TenantRequest request = await GetRequestAsync(input.Id);
			CheckPending(request);
			await _requestManager.CheckTenancyNameIsFreeAsync(request.TenancyName, request.Id);
			bool flag = input.EditionId.HasValue;
			if (flag)
			{
				flag = await _editionManager.FindByIdAsync(input.EditionId.Value) == null;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("EditionNotFound"));
			}
			Tenant tenant = await base.TenantManager.CreateWithAdminPasswordHashAsync(request.TenancyName, request.StoreName, request.AdminPasswordHash, request.AdminEmailAddress, input.IsActive, input.EditionId, request.AdminPhoneNumber, request.AdminName, request.AdminSurname);
			tenant.ContactName = request.ContactName;
			tenant.ContactEmail = request.ContactEmail;
			tenant.ContactPhone = request.ContactPhone;
			tenant.Address = request.Address;
			tenant.City = request.City;
			tenant.CountryName = request.CountryName;
			tenant.BusinessType = request.BusinessType;
			tenant.Notes = request.Notes;
			tenant.SubscriptionEndDate = input.SubscriptionEndDate;
			await base.CurrentUnitOfWork.SaveChangesAsync();
			if (input.FeatureOverrides != null && input.FeatureOverrides.Count > 0)
			{
				await _storeFeatureManager.SetTenantFeaturesAsync(tenant.Id, input.FeatureOverrides.Select((NameValueDto v) => new NameValue(v.Name, v.Value)));
			}
			_demoDataSeeder.SeedIfEnabled(tenant);
			request.Status = TenantRequestStatus.Approved;
			request.CreatedTenantId = tenant.Id;
			request.ReviewedByUserId = base.AbpSession.UserId;
			request.ReviewedTime = Clock.Now;
			request.ReviewNote = Clean(input.ReviewNote);
			request.AdminPasswordHash = null;
			await base.CurrentUnitOfWork.SaveChangesAsync();
			await _appNotifier.NewTenantRegisteredAsync(tenant);
			string tenancyName = tenant.TenancyName;
			AfterCommit(() => _emailer.SendApprovedAsync(request, tenancyName));
		}

		[AbpAuthorize(new string[] { "Pages.Tenants.Requests.Reject" })]
		public async Task Reject(RejectTenantRequestDto input)
		{
			UseHostSide();
			if (input.ReviewNote.IsNullOrWhiteSpace())
			{
				throw new UserFriendlyException(L("RejectionReasonIsRequired"));
			}
			StockPilot.MultiTenancy.TenantRequest request = await GetRequestAsync(input.Id);
			CheckPending(request);
			request.Status = TenantRequestStatus.Rejected;
			request.ReviewedByUserId = base.AbpSession.UserId;
			request.ReviewedTime = Clock.Now;
			request.ReviewNote = input.ReviewNote.Trim();
			request.AdminPasswordHash = null;
			AfterCommit(() => _emailer.SendRejectedAsync(request));
		}

		public async Task Delete(EntityDto input)
		{
			UseHostSide();
			StockPilot.MultiTenancy.TenantRequest request = await GetRequestAsync(input.Id);
			if (request.Status != TenantRequestStatus.Rejected && request.Status != TenantRequestStatus.Cancelled)
			{
				throw new UserFriendlyException(L("OnlyClosedTenantRequestsCanBeDeleted"));
			}
			await _requestRepository.DeleteAsync(request);
		}

		private void UseHostSide()
		{
			if (base.AbpSession.TenantId.HasValue)
			{
				throw new AbpAuthorizationException(L("TenantRequestsAreHostOnly"));
			}
			base.CurrentUnitOfWork.SetTenantId(null);
		}

		private async Task<StockPilot.MultiTenancy.TenantRequest> GetRequestAsync(int id)
		{
			StockPilot.MultiTenancy.TenantRequest obj = await _requestRepository.FirstOrDefaultAsync(id);
			if (obj == null)
			{
				throw new UserFriendlyException(L("TenantRequestNotFound"));
			}
			return obj;
		}

		private void CheckPending(StockPilot.MultiTenancy.TenantRequest request)
		{
			if (request.Status != TenantRequestStatus.Pending)
			{
				throw new UserFriendlyException(L("TenantRequestIsNotPending", request.RequestNo));
			}
		}

		private void AfterCommit(Func<Task> action)
		{
			base.CurrentUnitOfWork.Completed += delegate
			{
				Task.Run(action);
			};
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
