using Abp.Domain.Services;

namespace StockPilot
{
	public abstract class StockPilotDomainServiceBase : DomainService
	{
		protected StockPilotDomainServiceBase()
		{
			base.LocalizationSourceName = "StockPilot";
		}
	}
}
