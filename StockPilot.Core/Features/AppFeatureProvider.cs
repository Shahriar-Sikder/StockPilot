using Abp.Application.Features;
using Abp.Localization;
using Abp.Runtime.Validation;
using Abp.UI.Inputs;

namespace StockPilot.Features
{
	public class AppFeatureProvider : FeatureProvider
	{
		public override void SetFeatures(IFeatureDefinitionContext context)
		{
			Feature parent = Module(context, "App.Inventory.Purchases", "Purchases");
			Child(parent, "App.Inventory.Purchases.Attachments", "PurchaseAttachments");
			Child(parent, "App.Inventory.Purchases.ExcelImport", "PurchaseExcelImport");
			Module(context, "App.Inventory.Sales", "Sales");
			Module(context, "App.Inventory.Reports", "Reports");
			Module(context, "App.Inventory.Export", "Export");
			Module(context, "App.Inventory.MultiCurrency", "MultiCurrency");
			Module(context, "App.Inventory.UnitConversions", "UnitConversions");
			Module(context, "App.Inventory.Pictures", "Pictures");
			context.Create("App.HideCostAndValues", "false", L("HideCostAndValues"), L("HideCostAndValues_Description"), FeatureScopes.All, new CheckboxInputType());
			context.Create("App.MaxUserCount", "0", L("MaximumUserCount"), L("Feature_MaxUserCount_Description"), FeatureScopes.All, new SingleLineStringInputType(new NumericValueValidator(0)));
			context.Create("App.MaxItemCount", "0", L("Feature_MaxItemCount"), L("Feature_MaxItemCount_Description"), FeatureScopes.All, new SingleLineStringInputType(new NumericValueValidator(0)));
		}

		private static Feature Module(IFeatureDefinitionContext context, string name, string key)
		{
			return context.Create(name, "true", L("Feature_" + key), L("Feature_" + key + "_Description"), FeatureScopes.All, new CheckboxInputType());
		}

		private static Feature Child(Feature parent, string name, string key)
		{
			return parent.CreateChildFeature(name, "true", L("Feature_" + key), L("Feature_" + key + "_Description"), FeatureScopes.All, new CheckboxInputType());
		}

		private static ILocalizableString L(string name)
		{
			return new LocalizableString(name, "StockPilot");
		}
	}
}
