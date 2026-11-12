using System;
using System.Globalization;
using System.Threading;
using System.Web;
using Abp.Castle.Logging.Log4Net;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Logging;
using Abp.Timing;
using Abp.Web;
using Castle.Facilities.Logging;

namespace StockPilot.Web
{
	public class MvcApplication : AbpWebApplication<StockPilotWebModule>
	{
		private static readonly DateTime CacheExpireDate = new DateTime(2000, 1, 1);

		protected override void Application_Start(object sender, EventArgs e)
		{
			Clock.Provider = ClockProviders.Utc;
			AbpWebApplication<StockPilotWebModule>.AbpBootstrapper.IocManager.IocContainer.AddFacility(delegate(LoggingFacility f)
			{
				f.UseAbpLog4Net().WithConfig("log4net.config");
			});
			base.Application_Start(sender, e);
		}

		protected override void Session_Start(object sender, EventArgs e)
		{
			RestoreUserLanguage();
			base.Session_Start(sender, e);
		}

		private void RestoreUserLanguage()
		{
			HttpCookie chosen = base.Request.Cookies["Abp.Localization.CultureName"];
			if (chosen != null && !chosen.Value.IsNullOrWhiteSpace())
			{
				return;
			}
			string defaultLanguage = AbpWebApplication<StockPilotWebModule>.AbpBootstrapper.IocManager.Resolve<ISettingManager>().GetSettingValue("Abp.Localization.DefaultLanguageName");
			if (defaultLanguage.IsNullOrEmpty())
			{
				return;
			}
			try
			{
				CultureInfo.GetCultureInfo(defaultLanguage);
				base.Response.Cookies.Add(new HttpCookie("Abp.Localization.CultureName", defaultLanguage)
				{
					Expires = Clock.Now.AddYears(2)
				});
			}
			catch (CultureNotFoundException ex)
			{
				LogHelper.Logger.Warn(ex.Message, ex);
			}
		}

		protected override void Application_BeginRequest(object sender, EventArgs e)
		{
			base.Application_BeginRequest(sender, e);
			DisableClientCache();
		}

		private void DisableClientCache()
		{
			base.Response.Cache.SetCacheability(HttpCacheability.NoCache);
			base.Response.Cache.SetExpires(CacheExpireDate);
			base.Response.Cache.SetNoStore();
		}

		protected override void Application_PostAuthenticateRequest(object sender, EventArgs e)
		{
			base.Application_PostAuthenticateRequest(sender, e);
			HttpCookie languageCookie = base.Request.Cookies["Abp.Localization.CultureName"];
			if (languageCookie != null && !languageCookie.Value.IsNullOrWhiteSpace())
			{
				try
				{
					CultureInfo chosen = CultureInfo.GetCultureInfo(languageCookie.Value);
					Thread.CurrentThread.CurrentCulture = chosen;
					Thread.CurrentThread.CurrentUICulture = chosen;
				}
				catch (CultureNotFoundException)
				{
				}
			}
			if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "en")
			{
				CultureInfo customCulture = new CultureInfo("en-GB", useUserOverride: true);
				Thread.CurrentThread.CurrentCulture = customCulture;
				Thread.CurrentThread.CurrentUICulture = customCulture;
			}
		}
	}
}
