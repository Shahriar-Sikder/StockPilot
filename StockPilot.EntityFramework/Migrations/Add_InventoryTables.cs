using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Resources;

namespace StockPilot.Migrations
{
	[GeneratedCode("EntityFramework.Migrations", "6.4.4")]
	public sealed class Add_InventoryTables : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Add_InventoryTables));

		string IMigrationMetadata.Id => "202611050450028_Add_InventoryTables";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			CreateTable("dbo.Countries", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 10);
				ColumnModel name = c.String(false, 100);
				int? maxLength = 10;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					PhoneCode = c.String(null, maxLength),
					IsActive = c.Boolean(false),
					Sequence = c.Int(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_Country_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Country_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.Currencies", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 10);
				ColumnModel name = c.String(false, 100);
				int? maxLength = 10;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					Symbol = c.String(null, maxLength),
					DecimalPlaces = c.Int(false),
					IsBaseCurrency = c.Boolean(false),
					IsActive = c.Boolean(false),
					Sequence = c.Int(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_Currency_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Currency_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.CurrencyConversions", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(false),
				FromCurrencyId = c.Int(false),
				ToCurrencyId = c.Int(false),
				Rate = c.Decimal(false, (byte)18, (byte)2),
				EffectiveDate = c.DateTime(false),
				IsActive = c.Boolean(false),
				Sequence = c.Int(false),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_CurrencyConversion_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_CurrencyConversion_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.Currencies", t => t.FromCurrencyId, cascadeDelete: true).ForeignKey("dbo.Currencies", t => t.ToCurrencyId)
				.Index(t => t.FromCurrencyId)
				.Index(t => t.ToCurrencyId);
			CreateTable("dbo.IssueSales", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel documentNo = c.String(false, 50);
				ColumnModel documentDate = c.DateTime(false);
				ColumnModel issueType = c.Int(false);
				int? maxLength = 200;
				ColumnModel customerName = c.String(null, maxLength);
				maxLength = 100;
				return new
				{
					Id = id,
					TenantId = tenantId,
					DocumentNo = documentNo,
					DocumentDate = documentDate,
					IssueType = issueType,
					CustomerName = customerName,
					ReferenceNo = c.String(null, maxLength),
					CurrencyId = c.Int(false),
					ExchangeRate = c.Decimal(false, (byte)18, (byte)2),
					SubTotal = c.Decimal(false, (byte)18, (byte)2),
					DiscountAmount = c.Decimal(false, (byte)18, (byte)2),
					TotalAmount = c.Decimal(false, (byte)18, (byte)2),
					Status = c.Int(false),
					IsPosted = c.Boolean(false),
					PostedBy = c.Long(),
					PostedTime = c.DateTime(),
					Remarks = c.String(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_IssueSale_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_IssueSale_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.Currencies", t => t.CurrencyId, cascadeDelete: true).Index(t => t.CurrencyId);
			CreateTable("dbo.IssueSalesItems", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel issueSaleId = c.Int(false);
				ColumnModel itemMasterId = c.Int(false);
				ColumnModel unitMasterId = c.Int(false);
				ColumnModel quantity = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel conversionFactor = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel baseQuantity = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel unitPrice = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel unitCost = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel discountPercent = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel discountAmount = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel lineTotal = c.Decimal(false, (byte)18, (byte)2);
				int? maxLength = 500;
				return new
				{
					Id = id,
					TenantId = tenantId,
					IssueSaleId = issueSaleId,
					ItemMasterId = itemMasterId,
					UnitMasterId = unitMasterId,
					Quantity = quantity,
					ConversionFactor = conversionFactor,
					BaseQuantity = baseQuantity,
					UnitPrice = unitPrice,
					UnitCost = unitCost,
					DiscountPercent = discountPercent,
					DiscountAmount = discountAmount,
					LineTotal = lineTotal,
					Remarks = c.String(null, maxLength),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_IssueSalesItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_IssueSalesItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.IssueSales", t => t.IssueSaleId, cascadeDelete: true).ForeignKey("dbo.ItemMasters", t => t.ItemMasterId, cascadeDelete: true)
				.ForeignKey("dbo.UnitMasters", t => t.UnitMasterId, cascadeDelete: true)
				.Index(t => t.IssueSaleId)
				.Index(t => t.ItemMasterId)
				.Index(t => t.UnitMasterId);
			CreateTable("dbo.ItemMasters", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel itemCode = c.String(false, 50);
				ColumnModel itemName = c.String(false, 250);
				int? maxLength = 1000;
				ColumnModel description = c.String(null, maxLength);
				maxLength = 100;
				return new
				{
					Id = id,
					TenantId = tenantId,
					ItemCode = itemCode,
					ItemName = itemName,
					Description = description,
					Barcode = c.String(null, maxLength),
					ItemCategoryId = c.Int(false),
					ItemSubCategoryId = c.Int(),
					UnitSetId = c.Int(false),
					BaseUnitId = c.Int(false),
					PurchaseUnitId = c.Int(),
					SalesUnitId = c.Int(),
					SalePrice = c.Decimal(false, (byte)18, (byte)2),
					ReorderLevel = c.Decimal(false, (byte)18, (byte)2),
					MinStock = c.Decimal(false, (byte)18, (byte)2),
					MaxStock = c.Decimal(false, (byte)18, (byte)2),
					IsStockItem = c.Boolean(false),
					IsActive = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemMaster_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemMaster_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.UnitMasters", t => t.BaseUnitId).ForeignKey("dbo.ItemCategories", t => t.ItemCategoryId, cascadeDelete: true)
				.ForeignKey("dbo.ItemSubCategories", t => t.ItemSubCategoryId)
				.ForeignKey("dbo.UnitMasters", t => t.PurchaseUnitId)
				.ForeignKey("dbo.UnitMasters", t => t.SalesUnitId)
				.ForeignKey("dbo.UnitSets", t => t.UnitSetId, cascadeDelete: true)
				.Index(t => t.ItemCategoryId)
				.Index(t => t.ItemSubCategoryId)
				.Index(t => t.UnitSetId)
				.Index(t => t.BaseUnitId)
				.Index(t => t.PurchaseUnitId)
				.Index(t => t.SalesUnitId);
			CreateTable("dbo.UnitMasters", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(false),
				Code = c.String(false, 20),
				Name = c.String(false, 100),
				UnitSetId = c.Int(false),
				IsBaseUnit = c.Boolean(false),
				IsActive = c.Boolean(false),
				Sequence = c.Int(false),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long(),
				UnitSet_Id = c.Int()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitMaster_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitMaster_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.UnitSets", t => t.UnitSet_Id).ForeignKey("dbo.UnitSets", t => t.UnitSetId)
				.Index(t => t.UnitSetId)
				.Index(t => t.UnitSet_Id);
			CreateTable("dbo.UnitSets", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 50);
				ColumnModel name = c.String(false, 100);
				int? maxLength = 500;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					Description = c.String(null, maxLength),
					IsActive = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitSet_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitSet_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.ItemCategories", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 50);
				ColumnModel name = c.String(false, 150);
				int? maxLength = 500;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					Description = c.String(null, maxLength),
					IsActive = c.Boolean(false),
					Sequence = c.Int(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemCategory_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemCategory_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.ItemSubCategories", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel itemCategoryId = c.Int(false);
				ColumnModel code = c.String(false, 50);
				ColumnModel name = c.String(false, 150);
				int? maxLength = 500;
				return new
				{
					Id = id,
					TenantId = tenantId,
					ItemCategoryId = itemCategoryId,
					Code = code,
					Name = name,
					Description = c.String(null, maxLength),
					Sequence = c.Int(false),
					IsActive = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemSubCategory_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemSubCategory_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.ItemCategories", t => t.ItemCategoryId, cascadeDelete: true).Index(t => t.ItemCategoryId);
			CreateTable("dbo.PurchaseInvoiceItems", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel purchaseInvoiceId = c.Int(false);
				ColumnModel itemMasterId = c.Int(false);
				ColumnModel unitMasterId = c.Int(false);
				ColumnModel quantity = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel conversionFactor = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel baseQuantity = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel unitCost = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel discountPercent = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel discountAmount = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel lineTotal = c.Decimal(false, (byte)18, (byte)2);
				int? maxLength = 500;
				return new
				{
					Id = id,
					TenantId = tenantId,
					PurchaseInvoiceId = purchaseInvoiceId,
					ItemMasterId = itemMasterId,
					UnitMasterId = unitMasterId,
					Quantity = quantity,
					ConversionFactor = conversionFactor,
					BaseQuantity = baseQuantity,
					UnitCost = unitCost,
					DiscountPercent = discountPercent,
					DiscountAmount = discountAmount,
					LineTotal = lineTotal,
					Remarks = c.String(null, maxLength),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoiceItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoiceItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.ItemMasters", t => t.ItemMasterId, cascadeDelete: true).ForeignKey("dbo.PurchaseInvoices", t => t.PurchaseInvoiceId, cascadeDelete: true)
				.ForeignKey("dbo.UnitMasters", t => t.UnitMasterId, cascadeDelete: true)
				.Index(t => t.PurchaseInvoiceId)
				.Index(t => t.ItemMasterId)
				.Index(t => t.UnitMasterId);
			CreateTable("dbo.PurchaseInvoices", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel invoiceNo = c.String(false, 50);
				ColumnModel invoiceDate = c.DateTime(false);
				ColumnModel supplierId = c.Int(false);
				int? maxLength = 50;
				return new
				{
					Id = id,
					TenantId = tenantId,
					InvoiceNo = invoiceNo,
					InvoiceDate = invoiceDate,
					SupplierId = supplierId,
					SupplierInvoiceNo = c.String(null, maxLength),
					DueDate = c.DateTime(),
					CurrencyId = c.Int(false),
					ExchangeRate = c.Decimal(false, (byte)18, (byte)2),
					SubTotal = c.Decimal(false, (byte)18, (byte)2),
					DiscountAmount = c.Decimal(false, (byte)18, (byte)2),
					TaxAmount = c.Decimal(false, (byte)18, (byte)2),
					OtherCharges = c.Decimal(false, (byte)18, (byte)2),
					TotalAmount = c.Decimal(false, (byte)18, (byte)2),
					Status = c.Int(false),
					IsPosted = c.Boolean(false),
					PostedBy = c.Long(),
					PostedTime = c.DateTime(),
					Remarks = c.String(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoice_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoice_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.Currencies", t => t.CurrencyId, cascadeDelete: true).ForeignKey("dbo.Suppliers", t => t.SupplierId, cascadeDelete: true)
				.Index(t => t.SupplierId)
				.Index(t => t.CurrencyId);
			CreateTable("dbo.Suppliers", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 50);
				ColumnModel name = c.String(false, 200);
				int? maxLength = 150;
				ColumnModel contactPerson = c.String(null, maxLength);
				maxLength = 150;
				ColumnModel email = c.String(null, maxLength);
				maxLength = 50;
				ColumnModel phone = c.String(null, maxLength);
				maxLength = 500;
				ColumnModel address = c.String(null, maxLength);
				maxLength = 100;
				ColumnModel city = c.String(null, maxLength);
				ColumnModel countryId = c.Int();
				ColumnModel currencyId = c.Int();
				maxLength = 50;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					ContactPerson = contactPerson,
					Email = email,
					Phone = phone,
					Address = address,
					City = city,
					CountryId = countryId,
					CurrencyId = currencyId,
					TaxNumber = c.String(null, maxLength),
					PaymentTermDays = c.Int(false),
					Sequence = c.Int(false),
					IsActive = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_Supplier_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Supplier_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.Countries", t => t.CountryId).ForeignKey("dbo.Currencies", t => t.CurrencyId)
				.Index(t => t.CountryId)
				.Index(t => t.CurrencyId);
			CreateTable("dbo.StockBalances", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(false),
				ItemMasterId = c.Int(false),
				QuantityOnHand = c.Decimal(false, (byte)18, (byte)2),
				ReservedQuantity = c.Decimal(false, (byte)18, (byte)2),
				AverageCost = c.Decimal(false, (byte)18, (byte)2),
				LastPurchaseCost = c.Decimal(false, (byte)18, (byte)2),
				LastMovementTime = c.DateTime(),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockBalance_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockBalance_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.ItemMasters", t => t.ItemMasterId, cascadeDelete: true).Index(t => t.ItemMasterId);
			CreateTable("dbo.StockMovements", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Long(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel itemMasterId = c.Int(false);
				ColumnModel movementDate = c.DateTime(false);
				ColumnModel movementType = c.Int(false);
				ColumnModel quantity = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel unitCost = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel totalValue = c.Decimal(false, (byte)18, (byte)2);
				ColumnModel balanceAfter = c.Decimal(false, (byte)18, (byte)2);
				int? maxLength = 50;
				ColumnModel referenceType = c.String(null, maxLength);
				ColumnModel referenceId = c.Int();
				ColumnModel referenceItemId = c.Int();
				maxLength = 50;
				return new
				{
					Id = id,
					TenantId = tenantId,
					ItemMasterId = itemMasterId,
					MovementDate = movementDate,
					MovementType = movementType,
					Quantity = quantity,
					UnitCost = unitCost,
					TotalValue = totalValue,
					BalanceAfter = balanceAfter,
					ReferenceType = referenceType,
					ReferenceId = referenceId,
					ReferenceItemId = referenceItemId,
					ReferenceNo = c.String(null, maxLength),
					Remarks = c.String(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockMovement_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockMovement_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.ItemMasters", t => t.ItemMasterId, cascadeDelete: true).Index(t => t.ItemMasterId);
			CreateTable("dbo.UnitConversions", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(false),
				FromUnitId = c.Int(false),
				ToUnitId = c.Int(false),
				Factor = c.Decimal(false, (byte)18, (byte)2),
				UnitSetId = c.Int(false),
				IsActive = c.Boolean(false),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitConversion_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitConversion_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.UnitMasters", t => t.FromUnitId, cascadeDelete: true).ForeignKey("dbo.UnitMasters", t => t.ToUnitId)
				.ForeignKey("dbo.UnitSets", t => t.UnitSetId, cascadeDelete: true)
				.Index(t => t.FromUnitId)
				.Index(t => t.ToUnitId)
				.Index(t => t.UnitSetId);
			DropTable("dbo.StockDocuments", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.StockItems", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
		}

		public override void Down()
		{
			CreateTable("dbo.StockItems", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 64);
				ColumnModel name = c.String(false, 128);
				int? maxLength = 32;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					Unit = c.String(null, maxLength),
					PurchasePrice = c.Decimal(false, (byte)18, (byte)2),
					SellingPrice = c.Decimal(false, (byte)18, (byte)2),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.StockDocuments", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel documentNumber = c.String(false, 32);
				ColumnModel documentType = c.String(false, 24);
				ColumnModel status = c.String(false, 24);
				ColumnModel documentDate = c.DateTime(false);
				int? maxLength = 128;
				ColumnModel partyName = c.String(null, maxLength);
				maxLength = 512;
				return new
				{
					Id = id,
					TenantId = tenantId,
					DocumentNumber = documentNumber,
					DocumentType = documentType,
					Status = status,
					DocumentDate = documentDate,
					PartyName = partyName,
					Note = c.String(null, maxLength),
					TotalAmount = c.Decimal(false, (byte)18, (byte)2),
					LinesJson = c.String(false),
					ApprovedByUserId = c.Long(),
					ApprovedTime = c.DateTime(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			DropForeignKey("dbo.UnitConversions", "UnitSetId", "dbo.UnitSets");
			DropForeignKey("dbo.UnitConversions", "ToUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.UnitConversions", "FromUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.StockMovements", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.StockBalances", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.PurchaseInvoiceItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.PurchaseInvoices", "SupplierId", "dbo.Suppliers");
			DropForeignKey("dbo.Suppliers", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.Suppliers", "CountryId", "dbo.Countries");
			DropForeignKey("dbo.PurchaseInvoiceItems", "PurchaseInvoiceId", "dbo.PurchaseInvoices");
			DropForeignKey("dbo.PurchaseInvoices", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.PurchaseInvoiceItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.IssueSalesItems", "UnitMasterId", "dbo.UnitMasters");
			DropForeignKey("dbo.IssueSalesItems", "ItemMasterId", "dbo.ItemMasters");
			DropForeignKey("dbo.ItemMasters", "UnitSetId", "dbo.UnitSets");
			DropForeignKey("dbo.ItemMasters", "SalesUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.ItemMasters", "PurchaseUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.ItemMasters", "ItemSubCategoryId", "dbo.ItemSubCategories");
			DropForeignKey("dbo.ItemMasters", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.ItemSubCategories", "ItemCategoryId", "dbo.ItemCategories");
			DropForeignKey("dbo.ItemMasters", "BaseUnitId", "dbo.UnitMasters");
			DropForeignKey("dbo.UnitMasters", "UnitSetId", "dbo.UnitSets");
			DropForeignKey("dbo.UnitMasters", "UnitSet_Id", "dbo.UnitSets");
			DropForeignKey("dbo.IssueSalesItems", "IssueSaleId", "dbo.IssueSales");
			DropForeignKey("dbo.IssueSales", "CurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.CurrencyConversions", "ToCurrencyId", "dbo.Currencies");
			DropForeignKey("dbo.CurrencyConversions", "FromCurrencyId", "dbo.Currencies");
			DropIndex("dbo.UnitConversions", new string[1] { "UnitSetId" });
			DropIndex("dbo.UnitConversions", new string[1] { "ToUnitId" });
			DropIndex("dbo.UnitConversions", new string[1] { "FromUnitId" });
			DropIndex("dbo.StockMovements", new string[1] { "ItemMasterId" });
			DropIndex("dbo.StockBalances", new string[1] { "ItemMasterId" });
			DropIndex("dbo.Suppliers", new string[1] { "CurrencyId" });
			DropIndex("dbo.Suppliers", new string[1] { "CountryId" });
			DropIndex("dbo.PurchaseInvoices", new string[1] { "CurrencyId" });
			DropIndex("dbo.PurchaseInvoices", new string[1] { "SupplierId" });
			DropIndex("dbo.PurchaseInvoiceItems", new string[1] { "UnitMasterId" });
			DropIndex("dbo.PurchaseInvoiceItems", new string[1] { "ItemMasterId" });
			DropIndex("dbo.PurchaseInvoiceItems", new string[1] { "PurchaseInvoiceId" });
			DropIndex("dbo.ItemSubCategories", new string[1] { "ItemCategoryId" });
			DropIndex("dbo.UnitMasters", new string[1] { "UnitSet_Id" });
			DropIndex("dbo.UnitMasters", new string[1] { "UnitSetId" });
			DropIndex("dbo.ItemMasters", new string[1] { "SalesUnitId" });
			DropIndex("dbo.ItemMasters", new string[1] { "PurchaseUnitId" });
			DropIndex("dbo.ItemMasters", new string[1] { "BaseUnitId" });
			DropIndex("dbo.ItemMasters", new string[1] { "UnitSetId" });
			DropIndex("dbo.ItemMasters", new string[1] { "ItemSubCategoryId" });
			DropIndex("dbo.ItemMasters", new string[1] { "ItemCategoryId" });
			DropIndex("dbo.IssueSalesItems", new string[1] { "UnitMasterId" });
			DropIndex("dbo.IssueSalesItems", new string[1] { "ItemMasterId" });
			DropIndex("dbo.IssueSalesItems", new string[1] { "IssueSaleId" });
			DropIndex("dbo.IssueSales", new string[1] { "CurrencyId" });
			DropIndex("dbo.CurrencyConversions", new string[1] { "ToCurrencyId" });
			DropIndex("dbo.CurrencyConversions", new string[1] { "FromCurrencyId" });
			DropTable("dbo.UnitConversions", new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitConversion_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitConversion_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.StockMovements", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockMovement_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockMovement_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.StockBalances", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockBalance_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockBalance_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.Suppliers", new Dictionary<string, object>
			{
				{ "DynamicFilter_Supplier_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Supplier_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.PurchaseInvoices", new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoice_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoice_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.PurchaseInvoiceItems", new Dictionary<string, object>
			{
				{ "DynamicFilter_PurchaseInvoiceItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_PurchaseInvoiceItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.ItemSubCategories", new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemSubCategory_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemSubCategory_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.ItemCategories", new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemCategory_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemCategory_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.UnitSets", new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitSet_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitSet_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.UnitMasters", new Dictionary<string, object>
			{
				{ "DynamicFilter_UnitMaster_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UnitMaster_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.ItemMasters", new Dictionary<string, object>
			{
				{ "DynamicFilter_ItemMaster_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ItemMaster_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.IssueSalesItems", new Dictionary<string, object>
			{
				{ "DynamicFilter_IssueSalesItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_IssueSalesItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.IssueSales", new Dictionary<string, object>
			{
				{ "DynamicFilter_IssueSale_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_IssueSale_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.CurrencyConversions", new Dictionary<string, object>
			{
				{ "DynamicFilter_CurrencyConversion_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_CurrencyConversion_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.Currencies", new Dictionary<string, object>
			{
				{ "DynamicFilter_Currency_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Currency_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.Countries", new Dictionary<string, object>
			{
				{ "DynamicFilter_Country_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Country_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
		}
	}
}
