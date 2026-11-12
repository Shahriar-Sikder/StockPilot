using Abp.WebApi.Controllers;

namespace StockPilot.WebApi
{
	public abstract class StockPilotApiControllerBase : AbpApiController
	{
		protected StockPilotApiControllerBase()
		{
			base.LocalizationSourceName = "StockPilot";
		}
	}
}
