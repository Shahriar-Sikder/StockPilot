using System.Web.Mvc;
using Abp.Application.Navigation;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Abp.Threading;
using StockPilot.MultiTenancy;
using StockPilot.Sessions.Dto;
using StockPilot.Web.Models.Layout;
using StockPilot.Web.MultiTenancy;
using StockPilot.Web.Session;

namespace StockPilot.Web.Controllers
{
	public class LayoutController : StockPilotControllerBase
	{
		private readonly IPerRequestSessionCache _sessionCache;

		private readonly IUserNavigationManager _userNavigationManager;

		private readonly IMultiTenancyConfig _multiTenancyConfig;

		private readonly ILanguageManager _languageManager;

		private readonly ITenancyNameFinder _tenancyNameFinder;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		private readonly TenantManager _tenantManager;

		public LayoutController(IPerRequestSessionCache sessionCache, IUserNavigationManager userNavigationManager, IMultiTenancyConfig multiTenancyConfig, ILanguageManager languageManager, ITenancyNameFinder tenancyNameFinder, IUnitOfWorkManager unitOfWorkManager, TenantManager tenantManager)
		{
			_sessionCache = sessionCache;
			_userNavigationManager = userNavigationManager;
			_multiTenancyConfig = multiTenancyConfig;
			_languageManager = languageManager;
			_tenancyNameFinder = tenancyNameFinder;
			_unitOfWorkManager = unitOfWorkManager;
			_tenantManager = tenantManager;
		}

		[ChildActionOnly]
		public PartialViewResult LanguageSwitcher()
		{
			return PartialView("~/Views/Layout/_LanguageSwitcher.cshtml", new HeaderViewModel
			{
				Languages = _languageManager.GetLanguages(),
				CurrentLanguage = _languageManager.CurrentLanguage
			});
		}

		[ChildActionOnly]
		public PartialViewResult Header(string currentPageName = "")
		{
			HeaderViewModel headerModel = new HeaderViewModel();
			if (base.AbpSession.UserId.HasValue)
			{
				headerModel.LoginInformations = AsyncHelper.RunSync(() => _sessionCache.GetCurrentLoginInformationsAsync());
			}
			headerModel.Languages = _languageManager.GetLanguages();
			headerModel.CurrentLanguage = _languageManager.CurrentLanguage;
			headerModel.Menu = AsyncHelper.RunSync(() => _userNavigationManager.GetMenuAsync("Frontend", base.AbpSession.ToUserIdentifier()));
			headerModel.CurrentPageName = currentPageName;
			headerModel.IsMultiTenancyEnabled = _multiTenancyConfig.IsEnabled;
			headerModel.TenantRegistrationEnabled = base.SettingManager.GetSettingValue<bool>("App.TenantManagement.AllowSelfRegistration");
			return PartialView("~/Views/Layout/_Header.cshtml", headerModel);
		}

		[ChildActionOnly]
		public PartialViewResult TenantCustomCss()
		{
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (string.IsNullOrEmpty(tenancyName))
			{
				if (base.AbpSession.UserId.HasValue)
				{
					GetCurrentLoginInformationsOutput sessionInfo = AsyncHelper.RunSync(() => _sessionCache.GetCurrentLoginInformationsAsync());
					return PartialView("_TenantCustomCss", new TenantCustomCssViewModel
					{
						CustomCssId = sessionInfo.Tenant?.CustomCssId
					});
				}
				return PartialView("_TenantCustomCss", new TenantCustomCssViewModel
				{
					CustomCssId = null
				});
			}
			using (_unitOfWorkManager.Begin())
			{
				using (_unitOfWorkManager.Current.SetTenantId(null))
				{
					Tenant tenant = _tenantManager.FindByTenancyName(tenancyName);
					return PartialView("_TenantCustomCss", new TenantCustomCssViewModel
					{
						CustomCssId = tenant?.CustomCssId
					});
				}
			}
		}

		[ChildActionOnly]
		public PartialViewResult AppLogo(string appPath, int width = 168, int height = 33)
		{
			base.ViewBag.TenantCustomLogoWidth = width;
			base.ViewBag.TenantCustomLogoHeight = height;
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (string.IsNullOrEmpty(tenancyName))
			{
				if (!base.AbpSession.TenantId.HasValue)
				{
					base.ViewBag.TenantCustomLogoUrl = appPath + "Common/Images/app-logo-on-light.png";
				}
				else
				{
					using (_unitOfWorkManager.Begin())
					{
						using (_unitOfWorkManager.Current.SetTenantId(null))
						{
							Tenant tenant = _tenantManager.FindById(base.AbpSession.GetTenantId());
							SetLogoUrlForTenant(appPath, tenant);
						}
					}
				}
			}
			else
			{
				using (_unitOfWorkManager.Begin())
				{
					using (_unitOfWorkManager.Current.SetTenantId(null))
					{
						Tenant tenant2 = _tenantManager.FindByTenancyName(tenancyName);
						SetLogoUrlForTenant(appPath, tenant2);
					}
				}
			}
			return PartialView("~/Views/Account/_AppLogo.cshtml");
		}

		private void SetLogoUrlForTenant(string appPath, Tenant tenant)
		{
			base.ViewBag.TenantCustomLogoUrl = ((tenant != null && tenant.LogoId.HasValue) ? (appPath + "TenantCustomization/GetLogo") : (appPath + "Common/Images/app-logo-on-light.png"));
		}
	}
}
