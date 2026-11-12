namespace StockPilot.Web.MultiTenancy
{
	public interface ITenancyNameFinder
	{
		string GetCurrentTenancyNameOrNull();
	}
}
