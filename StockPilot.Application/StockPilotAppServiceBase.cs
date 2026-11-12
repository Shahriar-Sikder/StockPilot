using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.IdentityFramework;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Microsoft.AspNet.Identity;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot
{
	public abstract class StockPilotAppServiceBase : ApplicationService
	{
		public TenantManager TenantManager { get; set; }

		public UserManager UserManager { get; set; }

		protected StockPilotAppServiceBase()
		{
			base.LocalizationSourceName = "StockPilot";
		}

		protected virtual async Task<User> GetCurrentUserAsync()
		{
			User obj = await UserManager.FindByIdAsync(base.AbpSession.GetUserId());
			if (obj == null)
			{
				throw new ApplicationException("There is no current user!");
			}
			return obj;
		}

		protected virtual User GetCurrentUser()
		{
			User user = UserManager.FindById(base.AbpSession.GetUserId());
			if (user == null)
			{
				throw new ApplicationException("There is no current user!");
			}
			return user;
		}

		protected virtual Task<Tenant> GetCurrentTenantAsync()
		{
			using (base.CurrentUnitOfWork.SetTenantId(null))
			{
				return TenantManager.GetByIdAsync(base.AbpSession.GetTenantId());
			}
		}

		protected virtual Tenant GetCurrentTenant()
		{
			using (base.CurrentUnitOfWork.SetTenantId(null))
			{
				return TenantManager.GetById(base.AbpSession.GetTenantId());
			}
		}

		protected virtual void CheckErrors(IdentityResult identityResult)
		{
			identityResult.CheckErrors(base.LocalizationManager);
		}
	}
}
