using Abp;

namespace StockPilot
{
	public abstract class StockPilotServiceBase : AbpServiceBase
	{
		protected StockPilotServiceBase()
		{
			base.LocalizationSourceName = "StockPilot";
		}
	}
}
