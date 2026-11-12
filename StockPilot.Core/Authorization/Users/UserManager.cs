using System;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Localization;
using Abp.Organizations;
using Abp.Runtime.Caching;
using Abp.Threading;
using StockPilot.Authorization.Roles;
using StockPilot.Identity;

namespace StockPilot.Authorization.Users
{
	public class UserManager : AbpUserManager<Role, User>
	{
		private readonly IUnitOfWorkManager _unitOfWorkManager;

		public UserManager(UserStore userStore, RoleManager roleManager, IPermissionManager permissionManager, IUnitOfWorkManager unitOfWorkManager, ICacheManager cacheManager, IRepository<OrganizationUnit, long> organizationUnitRepository, IRepository<UserOrganizationUnit, long> userOrganizationUnitRepository, IOrganizationUnitSettings organizationUnitSettings, IdentityEmailMessageService emailService, ILocalizationManager localizationManager, ISettingManager settingManager, IdentitySmsMessageService smsService, IUserTokenProviderAccessor userTokenProviderAccessor)
			: base((AbpUserStore<Role, User>)userStore, (AbpRoleManager<Role, User>)roleManager, permissionManager, unitOfWorkManager, cacheManager, organizationUnitRepository, userOrganizationUnitRepository, organizationUnitSettings, localizationManager, emailService, settingManager, userTokenProviderAccessor)
		{
			_unitOfWorkManager = unitOfWorkManager;
			base.SmsService = smsService;
		}

		public async Task<User> GetUserOrNullAsync(UserIdentifier userIdentifier)
		{
			using (_unitOfWorkManager.Begin())
			{
				using (_unitOfWorkManager.Current.SetTenantId(userIdentifier.TenantId))
				{
					return await FindByIdAsync(userIdentifier.UserId);
				}
			}
		}

		public User GetUserOrNull(UserIdentifier userIdentifier)
		{
			return AsyncHelper.RunSync(() => GetUserOrNullAsync(userIdentifier));
		}

		public async Task<User> GetUserAsync(UserIdentifier userIdentifier)
		{
			User obj = await GetUserOrNullAsync(userIdentifier);
			if (obj == null)
			{
				throw new ApplicationException("There is no user: " + userIdentifier);
			}
			return obj;
		}

		public User GetUser(UserIdentifier userIdentifier)
		{
			return AsyncHelper.RunSync(() => GetUserAsync(userIdentifier));
		}
	}
}
