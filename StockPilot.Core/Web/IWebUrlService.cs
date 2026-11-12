namespace StockPilot.Web
{
	public interface IWebUrlService
	{
		bool SupportsTenancyNameInUrl { get; }

		string GetSiteRootAddress(string tenancyName = null);
	}
}
