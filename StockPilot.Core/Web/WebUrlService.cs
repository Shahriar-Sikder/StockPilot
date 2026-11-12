using System.Configuration;
using Abp.Dependency;
using Abp.Extensions;

namespace StockPilot.Web
{
	public class WebUrlService : IWebUrlService, ITransientDependency
	{
		public const string TenancyNamePlaceHolder = "{TENANCY_NAME}";

		public static string WebSiteRootAddress => ConfigurationManager.AppSettings["WebSiteRootAddress"].EnsureEndsWith('/');

		public bool SupportsTenancyNameInUrl
		{
			get
			{
				string siteRootFormat = WebSiteRootAddress;
				if (!siteRootFormat.IsNullOrEmpty())
				{
					return siteRootFormat.Contains("{TENANCY_NAME}");
				}
				return false;
			}
		}

		public string GetSiteRootAddress(string tenancyName = null)
		{
			string siteRootFormat = WebSiteRootAddress;
			if (!siteRootFormat.Contains("{TENANCY_NAME}"))
			{
				return siteRootFormat;
			}
			if (siteRootFormat.Contains("{TENANCY_NAME}."))
			{
				siteRootFormat = siteRootFormat.Replace("{TENANCY_NAME}.", "{TENANCY_NAME}");
			}
			if (tenancyName.IsNullOrEmpty())
			{
				return siteRootFormat.Replace("{TENANCY_NAME}", "");
			}
			return siteRootFormat.Replace("{TENANCY_NAME}", tenancyName + ".");
		}
	}
}
