using System;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Abp;
using Abp.Configuration.Startup;
using Abp.Hangfire;
using Abp.IO;
using Abp.Modules;
using Abp.Runtime.Caching.Redis;
using Abp.Web.Mvc;
using Abp.Web.SignalR;
using Abp.Zero.Configuration;
using Castle.MicroKernel.Registration;
using Microsoft.Owin.Security;
using StockPilot.Web.App.Startup;
using StockPilot.Web.Bundling;
using StockPilot.Web.Navigation;
using StockPilot.Web.Routing;
using StockPilot.WebApi;

namespace StockPilot.Web
{
	[DependsOn(new Type[]
	{
		typeof(AbpWebMvcModule),
		typeof(AbpZeroOwinModule),
		typeof(StockPilotDataModule),
		typeof(StockPilotApplicationModule),
		typeof(StockPilotWebApiModule),
		typeof(AbpWebSignalRModule),
		typeof(AbpRedisCacheModule),
		typeof(AbpHangfireModule)
	})]
	public class StockPilotWebModule : AbpModule
	{
		public override void PreInitialize()
		{
			base.Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();
			base.Configuration.Navigation.Providers.Add<AppNavigationProvider>();
			base.Configuration.Navigation.Providers.Add<FrontEndNavigationProvider>();
			base.Configuration.Modules.AbpWebCommon().MultiTenancy.DomainFormat = WebUrlService.WebSiteRootAddress;
		}

		public override void Initialize()
		{
			base.IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
			base.IocManager.IocContainer.Register(Component.For<IAuthenticationManager>().UsingFactoryMethod(() => HttpContext.Current.GetOwinContext().Authentication).LifestyleTransient());
			AreaRegistration.RegisterAllAreas();
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleTable.Bundles.IgnoreList.Clear();
			CommonBundleConfig.RegisterBundles(BundleTable.Bundles);
			FrontEndBundleConfig.RegisterBundles(BundleTable.Bundles);
		}

		public override void PostInitialize()
		{
			HttpServerUtility server = HttpContext.Current.Server;
			AppFolders appFolders = base.IocManager.Resolve<AppFolders>();
			appFolders.SampleProfileImagesFolder = server.MapPath("~/Common/Images/SampleProfilePics");
			appFolders.TempFileDownloadFolder = server.MapPath("~/Temp/Downloads");
			appFolders.WebLogsFolder = server.MapPath("~/App_Data/Logs");
			appFolders.RootPath = server.MapPath("~");
			try
			{
				DirectoryHelper.CreateIfNotExists(appFolders.TempFileDownloadFolder);
				DirectoryHelper.CreateIfNotExists(appFolders.RootPath);
			}
			catch
			{
			}
			// One-time move of item / category / sub-category pictures to files; skipped once
			// StockPilot:PicturesMigratedToFiles is "true". Logs its counts and never stops the site from starting.
			base.IocManager.Resolve<StockPilot.Services.Common.PictureFileMigrator>().RunAtStartup();
		}
	}
}
