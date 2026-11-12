using System.Globalization;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNet.Identity;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot.Authorization
{
	public class LogInManager : AbpLogInManager<Tenant, Role, User>
	{
		private readonly ISettingManager _settingManager;

		private readonly ILocalizationManager _localizationManager;

		public LogInManager(UserManager userManager, IMultiTenancyConfig multiTenancyConfig, IRepository<Tenant> tenantRepository, IUnitOfWorkManager unitOfWorkManager, ISettingManager settingManager, IRepository<UserLoginAttempt, long> userLoginAttemptRepository, IUserManagementConfig userManagementConfig, IIocResolver iocResolver, RoleManager roleManager, ILocalizationManager localizationManager)
			: base((AbpUserManager<Role, User>)userManager, multiTenancyConfig, tenantRepository, unitOfWorkManager, settingManager, userLoginAttemptRepository, userManagementConfig, iocResolver, (AbpRoleManager<Role, User>)roleManager)
		{
			_settingManager = settingManager;
			_localizationManager = localizationManager;
		}

		public override async Task<AbpLoginResult<Tenant, User>> LoginAsync(string userNameOrEmailAddress, string plainPassword, string tenancyName = null, bool shouldLockout = true)
		{
			AbpLoginResult<Tenant, User> result = await base.LoginAsync(userNameOrEmailAddress, plainPassword, tenancyName, shouldLockout);
			await CheckSubscriptionAsync(result);
			return result;
		}

		public override async Task<AbpLoginResult<Tenant, User>> LoginAsync(UserLoginInfo login, string tenancyName = null)
		{
			AbpLoginResult<Tenant, User> result = await base.LoginAsync(login, tenancyName);
			await CheckSubscriptionAsync(result);
			return result;
		}

		private async Task CheckSubscriptionAsync(AbpLoginResult<Tenant, User> result)
		{
			if (result.Result == AbpLoginResultType.Success && !(result.Tenant == null))
			{
				int graceDays = await _settingManager.GetSettingValueForApplicationAsync<int>("App.Subscription.GraceDays");
				if (TenantSubscription.IsLoginBlocked(result.Tenant.SubscriptionEndDate, Clock.Now, graceDays))
				{
					throw new UserFriendlyException(string.Format(_localizationManager.GetSource("StockPilot").GetString("TenantSubscriptionExpired"), result.Tenant.SubscriptionEndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
				}
			}
		}
	}
}
