using Abp.Authorization;
using Abp.Authorization.Users;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;

namespace StockPilot.Authorization
{
	public class PermissionChecker : PermissionChecker<Role, User>
	{
		public PermissionChecker(UserManager userManager)
			: base((AbpUserManager<Role, User>)userManager)
		{
		}
	}
}
