using Abp.IdentityFramework;
using Abp.Web.Mvc.Controllers;
using Microsoft.AspNet.Identity;

namespace StockPilot.Web.Controllers
{
	public abstract class StockPilotControllerBase : AbpController
	{
		protected StockPilotControllerBase()
		{
			base.LocalizationSourceName = "StockPilot";
		}

		protected void CheckErrors(IdentityResult identityResult)
		{
			identityResult.CheckErrors(base.LocalizationManager);
		}
	}
}
