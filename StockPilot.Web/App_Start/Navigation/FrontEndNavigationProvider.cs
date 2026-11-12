using Abp.Application.Navigation;
using Abp.Localization;

namespace StockPilot.Web.Navigation
{
	public class FrontEndNavigationProvider : NavigationProvider
	{
		public const string MenuName = "Frontend";

		public override void SetNavigation(INavigationProviderContext context)
		{
			MenuDefinition frontEndMenu = new MenuDefinition("Frontend", new FixedLocalizableString("Frontend menu"));
			context.Manager.Menus["Frontend"] = frontEndMenu;
			frontEndMenu.AddItem(new MenuItemDefinition("Frontend.Home", L("HomePage"), null, "")).AddItem(new MenuItemDefinition("Frontend.About", L("AboutUs"), null, "About"));
		}

		private static ILocalizableString L(string name)
		{
			return new LocalizableString(name, "StockPilot");
		}
	}
}
