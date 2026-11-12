using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.Zero.Configuration;
using StockPilot.Authorization.Users;

namespace StockPilot.Authorization.Roles
{
	public class RoleManager : AbpRoleManager<Role, User>
	{
		public RoleManager(RoleStore store, IPermissionManager permissionManager, IRoleManagementConfig roleManagementConfig, ICacheManager cacheManager, IUnitOfWorkManager unitOfWorkManager)
			: base((AbpRoleStore<Role, User>)store, permissionManager, roleManagementConfig, cacheManager, unitOfWorkManager)
		{
		}
	}
}
