using Abp.Application.Navigation;
using Abp.Localization;

namespace StockPilot.Web.App.Startup
{
	public class AppNavigationProvider : NavigationProvider
	{
		public override void SetNavigation(INavigationProviderContext context)
		{
			context.Manager.MainMenu.AddItem(new MenuItemDefinition("Dashboard", L("Dashboard"), "icon-speedometer", "dashboard", requiresAuthentication: false, "Pages.StockPilot")).AddItem(new MenuItemDefinition("StockPilot.Items", L("Items"), "icon-tag", "items", requiresAuthentication: false, "Pages.StockPilot.Items")).AddItem(new MenuItemDefinition("StockPilot.Purchases", L("Purchases"), "icon-doc", "purchase-invoices", requiresAuthentication: false, "Pages.StockPilot.Purchases"))
				.AddItem(new MenuItemDefinition("StockPilot.Sales", L("SalesAndIssues"), "icon-share", "issues", requiresAuthentication: false, "Pages.StockPilot.Sales"))
				.AddItem(new MenuItemDefinition("StockPilot.Approvals", L("Approvals"), "icon-check", "approvals", requiresAuthentication: false, "Pages.StockPilot.Purchases.Post"))
				.AddItem(new MenuItemDefinition("StockPilot.StockHistory", L("StockHistory"), "icon-list", "stock-history", requiresAuthentication: false, "Pages.StockPilot.StockHistory"))
				.AddItem(new MenuItemDefinition("StockPilot.Suppliers", L("Suppliers"), "icon-people", "suppliers", requiresAuthentication: false, "Pages.StockPilot.Suppliers"))
				.AddItem(new MenuItemDefinition("StockPilot.Reports", L("Reports"), "icon-chart", "reports", requiresAuthentication: false, "Pages.StockPilot.Reports"))
				.AddItem(new MenuItemDefinition("StockPilot.Setup", L("Setup"), "icon-settings", "setup", requiresAuthentication: false, "Pages.StockPilot.Setup"))
				.AddItem(new MenuItemDefinition("Administration", L("Administration"), "icon-wrench", null, requiresAuthentication: false, "Pages.Administration").AddItem(new MenuItemDefinition("Administration.Roles", L("Roles"), "icon-briefcase", "roles", requiresAuthentication: false, "Pages.Administration.Roles")).AddItem(new MenuItemDefinition("Administration.Users", L("Users"), "icon-users", "users", requiresAuthentication: false, "Pages.Administration.Users")).AddItem(new MenuItemDefinition("Administration.AuditLogs", L("AuditLogs"), "icon-lock", "audit", requiresAuthentication: false, "Pages.Administration.AuditLogs"))
					.AddItem(new MenuItemDefinition("Administration.Settings", L("Settings"), "icon-settings", "settings", requiresAuthentication: false, "Pages.Administration.Tenant.Settings"))
					.AddItem(new MenuItemDefinition("Administration.Maintenance", L("Maintenance"), "icon-wrench", "maintenance", requiresAuthentication: false, "Pages.Administration.Host.Maintenance")));
			context.Manager.MainMenu.AddItem(new MenuItemDefinition("Host.Tenants", L("Tenants"), "icon-globe", "tenants", requiresAuthentication: false, "Pages.Tenants")).AddItem(new MenuItemDefinition("Host.Editions", L("Editions"), "icon-grid", "editions", requiresAuthentication: false, "Pages.Editions"));
		}

		private static ILocalizableString L(string name)
		{
			return new LocalizableString(name, "StockPilot");
		}
	}
}
