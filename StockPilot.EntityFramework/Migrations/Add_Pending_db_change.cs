using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Infrastructure;
using System.Resources;

namespace StockPilot.Migrations
{
	[GeneratedCode("EntityFramework.Migrations", "6.4.4")]
	public sealed class Add_Pending_db_change : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Add_Pending_db_change));

		string IMigrationMetadata.Id => "202611051835014_Add_Pending_db_change";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			DropForeignKey("dbo.CurrencyConversions", "FromCurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.IssueSales", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.IssueSalesItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.IssueSalesItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.ItemMasters", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.ItemMasters", "UnitSetId", "dbo.UnitSets");
			DropForeignKey("dbo.ItemSubCategories", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.PurchaseInvoiceItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.PurchaseInvoiceItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.PurchaseInvoices", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.PurchaseInvoices", "SupplierId", "dbo.Suppliers");
			DropForeignKey("dbo.StockBalances", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.StockMovements", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.UnitConversions", "FromUnitId", "dbo.UnitMasters");
			CreateTable("dbo.PurchaseInvoiceDocuments", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(false),
				PurchaseInvoiceId = c.Int(false),
				FileName = c.String(false, 255),
				ContentType = c.String(false, 100),
				SizeBytes = c.Long(false),
				BinaryObjectId = c.Guid(false),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoiceDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoiceDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.AppBinaryObjects", t => t.BinaryObjectId).ForeignKey("dbo.PurchaseInvoices", t => t.PurchaseInvoiceId, cascadeDelete: true)
				.Index(t => t.PurchaseInvoiceId)
				.Index(t => t.BinaryObjectId);
			AddColumn("dbo.IssueSales", "TaxAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AddColumn("dbo.ItemMasters", "DefaultSupplierId", (ColumnBuilder c) => c.Int());
			AddColumn("dbo.ItemMasters", "StandardCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AddColumn("dbo.ItemMasters", "TaxPercent", (ColumnBuilder c) => c.Decimal(false, (byte)9, (byte)4));
			AddColumn("dbo.ItemMasters", "PictureId", (ColumnBuilder c) => c.Guid());
			AddColumn("dbo.ItemCategories", "PictureId", (ColumnBuilder c) => c.Guid());
			AddColumn("dbo.ItemSubCategories", "PictureId", (ColumnBuilder c) => c.Guid());
			AddColumn("dbo.PurchaseInvoiceItems", "ExpiryDate", (ColumnBuilder c) => c.DateTime());
			AddColumn("dbo.PurchaseInvoiceItems", "TaxPercent", (ColumnBuilder c) => c.Decimal(false, (byte)9, (byte)4));
			AddColumn("dbo.PurchaseInvoiceItems", "TaxAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AddColumn("dbo.StockBalances", "RowVersion", delegate(ColumnBuilder c)
			{
				bool? nullable = false;
				bool? fixedLength = true;
				return c.Binary(nullable, null, fixedLength, null, null, timestamp: true, null, "rowversion");
			});
			AlterColumn("dbo.CurrencyConversions", "Rate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
			AlterColumn("dbo.IssueSales", "ExchangeRate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
			AlterColumn("dbo.IssueSales", "SubTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSales", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSales", "TotalAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSales", "Remarks", delegate(ColumnBuilder c)
			{
				int? maxLength = 1000;
				return c.String(null, maxLength);
			});
			AlterColumn("dbo.IssueSalesItems", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "BaseQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "UnitPrice", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "DiscountPercent", (ColumnBuilder c) => c.Decimal(false, (byte)9, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "LineTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.ItemMasters", "SalePrice", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.ItemMasters", "ReorderLevel", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.ItemMasters", "MinStock", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.ItemMasters", "MaxStock", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "BaseQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "DiscountPercent", (ColumnBuilder c) => c.Decimal(false, (byte)9, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoiceItems", "LineTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoices", "ExchangeRate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
			AlterColumn("dbo.PurchaseInvoices", "SubTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoices", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoices", "TaxAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoices", "OtherCharges", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.PurchaseInvoices", "TotalAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockBalances", "QuantityOnHand", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockBalances", "ReservedQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockBalances", "AverageCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockBalances", "LastPurchaseCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockMovements", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockMovements", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockMovements", "TotalValue", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.StockMovements", "BalanceAfter", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.UnitConversions", "Factor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
			CreateIndex("dbo.ItemMasters", "DefaultSupplierId");
			AddForeignKey("dbo.ItemMasters", "DefaultSupplierId", "dbo.Suppliers", "Id");
			AddForeignKey("dbo.CurrencyConversions", "FromCurrencyId", "dbo.Currencies", "Id");
			AddForeignKey("dbo.IssueSales", "CurrencyId", "dbo.Currencies", "Id");
			AddForeignKey("dbo.IssueSalesItems", "ItemMasterId", "dbo.ItemMasters", "Id");
			AddForeignKey("dbo.IssueSalesItems", "UnitMasterId", "dbo.UnitMasters", "Id");
			AddForeignKey("dbo.ItemMasters", "ItemCategoryId", "dbo.ItemCategories", "Id");
			AddForeignKey("dbo.ItemMasters", "UnitSetId", "dbo.UnitSets", "Id");
			AddForeignKey("dbo.ItemSubCategories", "ItemCategoryId", "dbo.ItemCategories", "Id");
			AddForeignKey("dbo.PurchaseInvoiceItems", "ItemMasterId", "dbo.ItemMasters", "Id");
			AddForeignKey("dbo.PurchaseInvoiceItems", "UnitMasterId", "dbo.UnitMasters", "Id");
			AddForeignKey("dbo.PurchaseInvoices", "CurrencyId", "dbo.Currencies", "Id");
			AddForeignKey("dbo.PurchaseInvoices", "SupplierId", "dbo.Suppliers", "Id");
			AddForeignKey("dbo.StockBalances", "ItemMasterId", "dbo.ItemMasters", "Id");
			AddForeignKey("dbo.StockMovements", "ItemMasterId", "dbo.ItemMasters", "Id");
			AddForeignKey("dbo.UnitConversions", "FromUnitId", "dbo.UnitMasters", "Id");
		}

		public override void Down()
		{
			DropForeignKey("dbo.UnitConversions", "FromUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.StockMovements", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.StockBalances", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.PurchaseInvoices", "SupplierId", "dbo.Suppliers");
			DropForeignKey("dbo.PurchaseInvoices", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.PurchaseInvoiceItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.PurchaseInvoiceItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.ItemSubCategories", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.ItemMasters", "UnitSetId", "dbo.UnitSets");
			DropForeignKey("dbo.ItemMasters", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.IssueSalesItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.IssueSalesItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.IssueSales", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.CurrencyConversions", "FromCurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.PurchaseInvoiceDocuments", "PurchaseInvoiceId", "dbo.PurchaseInvoices");
			DropForeignKey("dbo.PurchaseInvoiceDocuments", "BinaryObjectId", "dbo.AppBinaryObjects");
			DropForeignKey("dbo.ItemMasters", "DefaultSupplierId", "dbo.Suppliers");
			DropIndex("dbo.PurchaseInvoiceDocuments", new string[1] { "BinaryObjectId" });
			DropIndex("dbo.PurchaseInvoiceDocuments", new string[1] { "PurchaseInvoiceId" });
			DropIndex("dbo.ItemMasters", new string[1] { "DefaultSupplierId" });
			AlterColumn("dbo.UnitConversions", "Factor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockMovements", "BalanceAfter", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockMovements", "TotalValue", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockMovements", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockMovements", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockBalances", "LastPurchaseCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockBalances", "AverageCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockBalances", "ReservedQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.StockBalances", "QuantityOnHand", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "TotalAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "OtherCharges", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "TaxAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "SubTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoices", "ExchangeRate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "LineTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "DiscountPercent", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "BaseQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.PurchaseInvoiceItems", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.ItemMasters", "MaxStock", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.ItemMasters", "MinStock", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.ItemMasters", "ReorderLevel", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.ItemMasters", "SalePrice", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "LineTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "DiscountPercent", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "UnitCost", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "UnitPrice", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "BaseQuantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSalesItems", "Quantity", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSales", "Remarks", (ColumnBuilder c) => c.String());
			AlterColumn("dbo.IssueSales", "TotalAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSales", "DiscountAmount", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSales", "SubTotal", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.IssueSales", "ExchangeRate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			AlterColumn("dbo.CurrencyConversions", "Rate", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)2));
			DropColumn("dbo.StockBalances", "RowVersion");
			DropColumn("dbo.PurchaseInvoiceItems", "TaxAmount");
			DropColumn("dbo.PurchaseInvoiceItems", "TaxPercent");
			DropColumn("dbo.PurchaseInvoiceItems", "ExpiryDate");
			DropColumn("dbo.ItemSubCategories", "PictureId");
			DropColumn("dbo.ItemCategories", "PictureId");
			DropColumn("dbo.ItemMasters", "PictureId");
			DropColumn("dbo.ItemMasters", "TaxPercent");
			DropColumn("dbo.ItemMasters", "StandardCost");
			DropColumn("dbo.ItemMasters", "DefaultSupplierId");
			DropColumn("dbo.IssueSales", "TaxAmount");
			DropTable("dbo.PurchaseInvoiceDocuments", new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoiceDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoiceDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			AddForeignKey("dbo.UnitConversions", "FromUnitId", "dbo.UnitMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.StockMovements", "ItemMasterId", "dbo.ItemMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.StockBalances", "ItemMasterId", "dbo.ItemMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.PurchaseInvoices", "SupplierId", "dbo.Suppliers", "Id", cascadeDelete: true);
			AddForeignKey("dbo.PurchaseInvoices", "CurrencyId", "dbo.Currencies", "Id", cascadeDelete: true);
			AddForeignKey("dbo.PurchaseInvoiceItems", "UnitMasterId", "dbo.UnitMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.PurchaseInvoiceItems", "ItemMasterId", "dbo.ItemMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.ItemSubCategories", "ItemCategoryId", "dbo.ItemCategories", "Id", cascadeDelete: true);
			AddForeignKey("dbo.ItemMasters", "UnitSetId", "dbo.UnitSets", "Id", cascadeDelete: true);
			AddForeignKey("dbo.ItemMasters", "ItemCategoryId", "dbo.ItemCategories", "Id", cascadeDelete: true);
			AddForeignKey("dbo.IssueSalesItems", "UnitMasterId", "dbo.UnitMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.IssueSalesItems", "ItemMasterId", "dbo.ItemMasters", "Id", cascadeDelete: true);
			AddForeignKey("dbo.IssueSales", "CurrencyId", "dbo.Currencies", "Id", cascadeDelete: true);
			AddForeignKey("dbo.CurrencyConversions", "FromCurrencyId", "dbo.Currencies", "Id", cascadeDelete: true);
		}
	}
}
