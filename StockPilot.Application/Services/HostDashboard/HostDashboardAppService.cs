using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.ObjectMapping;
using Abp.Timing;
using StockPilot.Authorization.Users;
using StockPilot.Entity;
using StockPilot.MultiTenancy;
using StockPilot.Services.HostDashboard.Dtos;
using StockPilot.Services.TenantRequest.Dtos;

namespace StockPilot.Services.HostDashboard
{
	[AbpAuthorize(new string[] { "Pages.Tenants" })]
	public class HostDashboardAppService : StockPilotAppServiceBase, IHostDashboardAppService, IApplicationService, ITransientDependency
	{
		public const decimal NearLimitShare = 0.8m;

		public const int LatestRequestCount = 5;

		private readonly IRepository<StockPilot.MultiTenancy.TenantRequest> _requestRepository;

		private readonly IRepository<User, long> _userRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		public HostDashboardAppService(IRepository<StockPilot.MultiTenancy.TenantRequest> requestRepository, IRepository<User, long> userRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository)
		{
			_requestRepository = requestRepository;
			_userRepository = userRepository;
			_itemRepository = itemRepository;
		}

		public async Task<HostDashboardDto> GetSummary()
		{
			if (base.AbpSession.TenantId.HasValue)
			{
				throw new AbpAuthorizationException(L("TenantRequestsAreHostOnly"));
			}
			base.CurrentUnitOfWork.SetTenantId(null);
			DateTime today = Clock.Now.Date;
			int warnDays = await base.SettingManager.GetSettingValueForApplicationAsync<int>("App.Subscription.WarnBeforeDays");
			List<Tenant> tenants = await base.TenantManager.Tenants.ToListAsync();
			HostDashboardDto hostDashboardDto = new HostDashboardDto
			{
				ActiveTenants = tenants.Count((Tenant t) => t.IsActive),
				InactiveTenants = tenants.Count((Tenant t) => !t.IsActive)
			};
			HostDashboardDto hostDashboardDto2 = hostDashboardDto;
			hostDashboardDto2.PendingRequests = await _requestRepository.CountAsync((StockPilot.MultiTenancy.TenantRequest r) => (int)r.Status == 0);
			hostDashboardDto.WarnBeforeDays = warnDays;
			HostDashboardDto summary = hostDashboardDto;
			hostDashboardDto = summary;
			IObjectMapper objectMapper = base.ObjectMapper;
			hostDashboardDto.LatestPendingRequests = objectMapper.Map<List<TenantRequestListDto>>(await (from r in _requestRepository.GetAll()
				where (int)r.Status == 0
				orderby r.CreationTime descending
				select r).Take(5).ToListAsync());
			List<KeyValuePair<int, int>> users;
			List<KeyValuePair<int, int>> items;
			using (base.CurrentUnitOfWork.DisableFilter("MayHaveTenant", "MustHaveTenant"))
			{
				users = (await (from u in _userRepository.GetAll()
					where u.TenantId != (int?)null
					group u by u.TenantId.Value into g
					select new
					{
						Key = g.Key,
						Count = g.Count()
					}).ToListAsync()).Select(x => new KeyValuePair<int, int>(x.Key, x.Count)).ToList();
				items = (await (from i in _itemRepository.GetAll()
					where i.IsActive
					group i by i.TenantId into g
					select new
					{
						Key = g.Key,
						Count = g.Count()
					}).ToListAsync()).Select(x => new KeyValuePair<int, int>(x.Key, x.Count)).ToList();
			}
			foreach (Tenant tenant in tenants)
			{
				HostTenantAlertDto hostTenantAlertDto = new HostTenantAlertDto
				{
					TenantId = tenant.Id,
					TenancyName = tenant.TenancyName,
					Name = tenant.Name,
					SubscriptionEndDate = tenant.SubscriptionEndDate,
					UserCount = (from u in users
						where u.Key == tenant.Id
						select u.Value).FirstOrDefault(),
					ItemCount = (from i in items
						where i.Key == tenant.Id
						select i.Value).FirstOrDefault()
				};
				HostTenantAlertDto hostTenantAlertDto2 = hostTenantAlertDto;
				hostTenantAlertDto2.MaxUserCount = (await base.FeatureChecker.GetValueAsync(tenant.Id, "App.MaxUserCount")).To<int>();
				HostTenantAlertDto hostTenantAlertDto3 = hostTenantAlertDto;
				hostTenantAlertDto3.MaxItemCount = (await base.FeatureChecker.GetValueAsync(tenant.Id, "App.MaxItemCount")).To<int>();
				HostTenantAlertDto alert = hostTenantAlertDto;
				if (TenantSubscription.GetStatus(tenant.SubscriptionEndDate, today, warnDays) == TenantSubscriptionStatus.ExpiringSoon)
				{
					summary.ExpiringSoon++;
					summary.TenantsExpiringSoon.Add(alert);
				}
				if (IsNear(alert.UserCount, alert.MaxUserCount) || IsNear(alert.ItemCount, alert.MaxItemCount))
				{
					summary.TenantsNearLimit.Add(alert);
				}
			}
			summary.TenantsExpiringSoon = summary.TenantsExpiringSoon.OrderBy((HostTenantAlertDto t) => t.SubscriptionEndDate).ToList();
			summary.TenantsNearLimit = summary.TenantsNearLimit.OrderBy((HostTenantAlertDto t) => t.TenancyName).ToList();
			return summary;
		}

		private static bool IsNear(int used, int limit)
		{
			if (limit > 0)
			{
				return (decimal)used >= 0.8m * (decimal)limit;
			}
			return false;
		}
	}
}
