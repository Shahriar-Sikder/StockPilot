using System;
using System.Web;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Text;

namespace StockPilot.Web.MultiTenancy
{
	public class SubdomainTenancyNameFinder : ITenancyNameFinder, ITransientDependency
	{
		private readonly IMultiTenancyConfig _multiTenancyConfig;

		public SubdomainTenancyNameFinder(IMultiTenancyConfig multiTenancyConfig)
		{
			_multiTenancyConfig = multiTenancyConfig;
		}

		public string GetCurrentTenancyNameOrNull()
		{
			if (!_multiTenancyConfig.IsEnabled)
			{
				return "Default";
			}
			if (HttpContext.Current == null || HttpContext.Current.Request.Url == null)
			{
				return null;
			}
			if (!FormattedStringValueExtracter.IsMatch(GetCurrentSiteRootAddress().EnsureEndsWith('/'), WebUrlService.WebSiteRootAddress, out var values, ignoreCase: true))
			{
				return null;
			}
			if (values.Length == 0)
			{
				return null;
			}
			if (string.Equals(values[0], "www", StringComparison.InvariantCultureIgnoreCase))
			{
				return null;
			}
			return values[0];
		}

		private static string GetCurrentSiteRootAddress()
		{
			Uri url = HttpContext.Current.Request.Url;
			return url.Scheme + Uri.SchemeDelimiter + url.Host + (url.IsDefaultPort ? "" : (":" + url.Port)) + HttpContext.Current.Request.ApplicationPath;
		}
	}
}
