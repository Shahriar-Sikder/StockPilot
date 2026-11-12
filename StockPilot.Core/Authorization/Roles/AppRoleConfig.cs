using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace StockPilot.Authorization.Roles
{
	public static class AppRoleConfig
	{
		public static void Configure(IRoleManagementConfig roleManagementConfig)
		{
			roleManagementConfig.StaticRoles.Add(new StaticRoleDefinition("Admin", MultiTenancySides.Host));
			roleManagementConfig.StaticRoles.Add(new StaticRoleDefinition("Admin", MultiTenancySides.Tenant));
			roleManagementConfig.StaticRoles.Add(new StaticRoleDefinition("User", MultiTenancySides.Tenant));
		}
	}
}
