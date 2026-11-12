using Abp.Application.Features;
using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Localization;
using Abp.MultiTenancy;

namespace StockPilot.Authorization
{
	public class AppAuthorizationProvider : AuthorizationProvider
	{
		private readonly bool _isMultiTenancyEnabled;

		public AppAuthorizationProvider(bool isMultiTenancyEnabled)
		{
			_isMultiTenancyEnabled = isMultiTenancyEnabled;
		}

		public AppAuthorizationProvider(IMultiTenancyConfig multiTenancyConfig)
		{
			_isMultiTenancyEnabled = multiTenancyConfig.IsEnabled;
		}

		public override void SetPermissions(IPermissionDefinitionContext context)
		{
			Permission permission = context.CreatePermission("Pages", L("Pages"));
			Permission administration = permission.CreateChildPermission("Pages.Administration", L("Administration"));
			Permission permission2 = permission.CreateChildPermission("Pages.Tenants", L("Tenants"), null, MultiTenancySides.Host);
			permission2.CreateChildPermission("Pages.Tenants.Create", L("CreatingNewTenant"), null, MultiTenancySides.Host);
			permission2.CreateChildPermission("Pages.Tenants.Edit", L("EditingTenant"), null, MultiTenancySides.Host);
			permission2.CreateChildPermission("Pages.Tenants.Delete", L("DeletingTenant"), null, MultiTenancySides.Host);
			permission2.CreateChildPermission("Pages.Tenants.ChangeFeatures", L("ChangingFeatures"), null, MultiTenancySides.Host);
			permission2.CreateChildPermission("Pages.Tenants.Impersonation", L("LoginForTenants"), null, MultiTenancySides.Host);
			Permission permission3 = permission2.CreateChildPermission("Pages.Tenants.Requests", L("TenantRequests"), null, MultiTenancySides.Host);
			permission3.CreateChildPermission("Pages.Tenants.Requests.Approve", L("ApprovingTenantRequests"), null, MultiTenancySides.Host);
			permission3.CreateChildPermission("Pages.Tenants.Requests.Reject", L("RejectingTenantRequests"), null, MultiTenancySides.Host);
			Permission permission4 = permission.CreateChildPermission("Pages.Editions", L("Editions"), null, MultiTenancySides.Host);
			permission4.CreateChildPermission("Pages.Editions.Create", L("CreatingNewEdition"), null, MultiTenancySides.Host);
			permission4.CreateChildPermission("Pages.Editions.Edit", L("EditingEdition"), null, MultiTenancySides.Host);
			permission4.CreateChildPermission("Pages.Editions.Delete", L("DeletingEdition"), null, MultiTenancySides.Host);
			Permission permission5 = administration.CreateChildPermission("Pages.Administration.Users", L("Users"));
			permission5.CreateChildPermission("Pages.Administration.Users.Create", L("CreatingNewUser"));
			permission5.CreateChildPermission("Pages.Administration.Users.Edit", L("EditingUser"));
			permission5.CreateChildPermission("Pages.Administration.Users.Delete", L("DeletingUser"));
			permission5.CreateChildPermission("Pages.Administration.Users.ChangePermissions", L("ChangingPermissions"));
			permission5.CreateChildPermission("Pages.Administration.Users.Impersonation", L("UsersImpersonation"));
			Permission permission6 = administration.CreateChildPermission("Pages.Administration.Roles", L("Roles"));
			permission6.CreateChildPermission("Pages.Administration.Roles.Create", L("CreatingNewRole"));
			permission6.CreateChildPermission("Pages.Administration.Roles.Edit", L("EditingRole"));
			permission6.CreateChildPermission("Pages.Administration.Roles.Delete", L("DeletingRole"));
			Permission permission7 = administration.CreateChildPermission("Pages.Administration.Languages", L("Languages"));
			permission7.CreateChildPermission("Pages.Administration.Languages.Create", L("CreatingNewLanguage"));
			permission7.CreateChildPermission("Pages.Administration.Languages.Edit", L("EditingLanguage"));
			permission7.CreateChildPermission("Pages.Administration.Languages.ChangeTexts", L("ChangingTexts"));
			Permission permission8 = administration.CreateChildPermission("Pages.Administration.OrganizationUnits", L("OrganizationUnits"));
			permission8.CreateChildPermission("Pages.Administration.OrganizationUnits.ManageOrganizationTree", L("ManagingOrganizationTree"));
			permission8.CreateChildPermission("Pages.Administration.OrganizationUnits.ManageMembers", L("ManagingMembers"));
			administration.CreateChildPermission("Pages.Administration.AuditLogs", L("AuditLogs"));
			administration.CreateChildPermission("Pages.Administration.Host.Settings", L("Settings"), null, (!_isMultiTenancyEnabled) ? MultiTenancySides.Tenant : MultiTenancySides.Host);
			administration.CreateChildPermission("Pages.Administration.Host.Maintenance", L("Maintenance"), null, (!_isMultiTenancyEnabled) ? MultiTenancySides.Tenant : MultiTenancySides.Host);
			administration.CreateChildPermission("Pages.Administration.Tenant.Settings", L("Settings"), null, MultiTenancySides.Tenant);
			Permission stockPilot = permission.CreateChildPermission("Pages.StockPilot", L("Inventory"), null, MultiTenancySides.Tenant);
			CreateInventoryPermission(stockPilot, "Pages.StockPilot.Items", "Items", crud: true);
			CreateInventoryPermission(stockPilot, "Pages.StockPilot.Purchases", "Purchases", crud: true, posting: true, "App.Inventory.Purchases");
			CreateInventoryPermission(stockPilot, "Pages.StockPilot.Sales", "Sales & issues", crud: true, posting: true, "App.Inventory.Sales");
			stockPilot.CreateChildPermission("Pages.StockPilot.StockHistory", L("StockHistory"), null, MultiTenancySides.Tenant);
			CreateInventoryPermission(stockPilot, "Pages.StockPilot.Suppliers", "Suppliers", crud: true);
			stockPilot.CreateChildPermission("Pages.StockPilot.Reports", L("Reports"), null, MultiTenancySides.Tenant, new SimpleFeatureDependency("App.Inventory.Reports"));
			Permission parent = CreateInventoryPermission(stockPilot, "Pages.StockPilot.Setup", "Setup", crud: false);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.Categories", "Categories", crud: true);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.SubCategories", "Sub-categories", crud: true);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.UnitSets", "Unit groups", crud: true);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.Units", "Units", crud: true);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.UnitConversions", "Unit conversions", crud: true, posting: false, null, "App.Inventory.UnitConversions");
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.Currencies", "Currencies", crud: true);
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.CurrencyConversions", "Exchange rates", crud: true, posting: false, null, "App.Inventory.MultiCurrency");
			CreateInventoryPermission(parent, "Pages.StockPilot.Setup.Countries", "Countries", crud: true);
		}

		private static Permission CreateInventoryPermission(Permission parent, string name, string localizationKey, bool crud, bool posting = false, string feature = null, string actionFeature = null)
		{
			Permission permission = parent.CreateChildPermission(name, L(localizationKey), null, MultiTenancySides.Tenant, Dependency());
			if (crud)
			{
				permission.CreateChildPermission(name + ".Create", L("Permission_Create"), null, MultiTenancySides.Tenant, ActionDependency());
				permission.CreateChildPermission(name + ".Edit", L("Permission_Edit"), null, MultiTenancySides.Tenant, ActionDependency());
				permission.CreateChildPermission(name + ".Delete", L("Permission_Delete"), null, MultiTenancySides.Tenant, ActionDependency());
			}
			if (posting)
			{
				permission.CreateChildPermission(name + ".Post", L("Permission_Post"), L("Permission_Post_Description"), MultiTenancySides.Tenant, ActionDependency());
			}
			if (posting)
			{
				permission.CreateChildPermission(name + ".Cancel", L("Permission_Cancel"), L("Permission_Cancel_Description"), MultiTenancySides.Tenant, ActionDependency());
			}
			return permission;
			IFeatureDependency ActionDependency()
			{
				if ((feature ?? actionFeature) != null)
				{
					return new SimpleFeatureDependency(feature ?? actionFeature);
				}
				return null;
			}
			IFeatureDependency Dependency()
			{
				if (feature != null)
				{
					return new SimpleFeatureDependency(feature);
				}
				return null;
			}
		}

		private static ILocalizableString L(string name)
		{
			return new LocalizableString(name, "StockPilot");
		}
	}
}
