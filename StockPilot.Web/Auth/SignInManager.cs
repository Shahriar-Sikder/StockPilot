using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Uow;
using Microsoft.Owin.Security;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot.Web.Auth
{
	public class SignInManager : AbpSignInManager<Tenant, Role, User>
	{
		public SignInManager(UserManager userManager, IAuthenticationManager authenticationManager, ISettingManager settingManager, IUnitOfWorkManager unitOfWorkManager)
			: base((AbpUserManager<Role, User>)userManager, authenticationManager, settingManager, unitOfWorkManager)
		{
		}
	}
}
