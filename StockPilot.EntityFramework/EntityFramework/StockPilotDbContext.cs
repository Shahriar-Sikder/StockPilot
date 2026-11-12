using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using Abp.Zero.EntityFramework;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.Entity;
using StockPilot.MultiTenancy;
using StockPilot.Storage;

namespace StockPilot.EntityFramework
{
	public class StockPilotDbContext : AbpZeroDbContext<Tenant, Role, User>
	{
		public virtual IDbSet<BinaryObject> BinaryObjects { get; set; }

		public virtual DbSet<Country> Countries { get; set; }

		public virtual DbSet<Currency> Currencies { get; set; }

		public virtual DbSet<CurrencyConversion> CurrencyConversions { get; set; }

		public virtual DbSet<ItemCategory> ItemCategories { get; set; }

		public virtual DbSet<ItemSubCategory> ItemSubCategories { get; set; }

		public virtual DbSet<ItemMaster> ItemMasters { get; set; }

		public virtual DbSet<Supplier> Suppliers { get; set; }

		public virtual DbSet<UnitSet> UnitSets { get; set; }

		public virtual DbSet<UnitMaster> UnitMasters { get; set; }

		public virtual DbSet<UnitConversion> UnitConversions { get; set; }

		public virtual DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }

		public virtual DbSet<PurchaseInvoiceDocument> PurchaseInvoiceDocuments { get; set; }

		public virtual DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }

		public virtual DbSet<IssueSale> IssueSales { get; set; }

		public virtual DbSet<IssueSalesItem> IssueSalesItems { get; set; }

		public virtual DbSet<StockBalance> StockBalances { get; set; }

		public virtual DbSet<StockMovement> StockMovements { get; set; }

		public virtual DbSet<DocumentSequence> DocumentSequences { get; set; }

		public virtual DbSet<TenantRequest> TenantRequests { get; set; }

		public StockPilotDbContext()
			: base("Default")
		{
		}

		public StockPilotDbContext(string nameOrConnectionString)
			: base(nameOrConnectionString)
		{
		}

		public StockPilotDbContext(DbConnection existingConnection)
			: base(existingConnection, contextOwnsConnection: false)
		{
		}

		public StockPilotDbContext(DbConnection existingConnection, bool contextOwnsConnection)
			: base(existingConnection, contextOwnsConnection)
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<CurrencyConversion>().HasRequired((CurrencyConversion rr) => rr.FromCurrency).WithMany()
				.HasForeignKey((CurrencyConversion rr) => rr.FromCurrencyId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<CurrencyConversion>().HasRequired((CurrencyConversion rr) => rr.ToCurrency).WithMany()
				.HasForeignKey((CurrencyConversion rr) => rr.ToCurrencyId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasRequired((ItemMaster rr) => rr.BaseUnit).WithMany()
				.HasForeignKey((ItemMaster rr) => rr.BaseUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<UnitMaster>().HasRequired((UnitMaster rr) => rr.UnitSetInfo).WithMany((UnitSet set) => set.Units)
				.HasForeignKey((UnitMaster rr) => rr.UnitSetId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<UnitConversion>().HasRequired((UnitConversion rr) => rr.UnitSetInfo).WithMany()
				.HasForeignKey((UnitConversion rr) => rr.UnitSetId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<UnitConversion>().HasRequired((UnitConversion rr) => rr.FromUnit).WithMany()
				.HasForeignKey((UnitConversion rr) => rr.FromUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<UnitConversion>().HasRequired((UnitConversion rr) => rr.ToUnit).WithMany()
				.HasForeignKey((UnitConversion rr) => rr.ToUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<PurchaseInvoiceDocument>().HasRequired((PurchaseInvoiceDocument document) => document.PurchaseInvoiceInfo).WithMany((PurchaseInvoice invoice) => invoice.Documents)
				.HasForeignKey((PurchaseInvoiceDocument document) => document.PurchaseInvoiceId)
				.WillCascadeOnDelete(value: true);
			modelBuilder.Entity<PurchaseInvoiceDocument>().HasRequired((PurchaseInvoiceDocument document) => document.BinaryObjectInfo).WithMany()
				.HasForeignKey((PurchaseInvoiceDocument document) => document.BinaryObjectId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Properties<decimal>().Configure(delegate(ConventionPrimitivePropertyConfiguration property)
			{
				property.HasPrecision(18, 4);
			});
			modelBuilder.Entity<CurrencyConversion>().Property((CurrencyConversion entity) => entity.Rate).HasPrecision(18, 6);
			modelBuilder.Entity<UnitConversion>().Property((UnitConversion entity) => entity.Factor).HasPrecision(18, 6);
			modelBuilder.Entity<PurchaseInvoice>().Property((PurchaseInvoice entity) => entity.ExchangeRate).HasPrecision(18, 6);
			modelBuilder.Entity<IssueSale>().Property((IssueSale entity) => entity.ExchangeRate).HasPrecision(18, 6);
			modelBuilder.Entity<ItemMaster>().Property((ItemMaster entity) => entity.TaxPercent).HasPrecision(9, 4);
			modelBuilder.Entity<PurchaseInvoiceItem>().Property((PurchaseInvoiceItem entity) => entity.DiscountPercent).HasPrecision(9, 4);
			modelBuilder.Entity<PurchaseInvoiceItem>().Property((PurchaseInvoiceItem entity) => entity.TaxPercent).HasPrecision(9, 4);
			modelBuilder.Entity<IssueSalesItem>().Property((IssueSalesItem entity) => entity.DiscountPercent).HasPrecision(9, 4);
			modelBuilder.Entity<PurchaseInvoiceItem>().Property((PurchaseInvoiceItem entity) => entity.ConversionFactor).HasPrecision(18, 6);
			modelBuilder.Entity<IssueSalesItem>().Property((IssueSalesItem entity) => entity.ConversionFactor).HasPrecision(18, 6);
			modelBuilder.Entity<PurchaseInvoiceItem>().HasRequired((PurchaseInvoiceItem item) => item.PurchaseInvoiceInfo).WithMany((PurchaseInvoice invoice) => invoice.Items)
				.HasForeignKey((PurchaseInvoiceItem item) => item.PurchaseInvoiceId)
				.WillCascadeOnDelete(value: true);
			modelBuilder.Entity<PurchaseInvoiceItem>().HasRequired((PurchaseInvoiceItem item) => item.ItemMasterInfo).WithMany()
				.HasForeignKey((PurchaseInvoiceItem item) => item.ItemMasterId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<PurchaseInvoiceItem>().HasRequired((PurchaseInvoiceItem item) => item.UnitMasterInfo).WithMany()
				.HasForeignKey((PurchaseInvoiceItem item) => item.UnitMasterId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<IssueSale>().HasRequired((IssueSale document) => document.CurrencyInfo).WithMany()
				.HasForeignKey((IssueSale document) => document.CurrencyId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<IssueSalesItem>().HasRequired((IssueSalesItem item) => item.IssueSaleInfo).WithMany((IssueSale document) => document.Items)
				.HasForeignKey((IssueSalesItem item) => item.IssueSaleId)
				.WillCascadeOnDelete(value: true);
			modelBuilder.Entity<IssueSalesItem>().HasRequired((IssueSalesItem item) => item.ItemMasterInfo).WithMany()
				.HasForeignKey((IssueSalesItem item) => item.ItemMasterId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<IssueSalesItem>().HasRequired((IssueSalesItem item) => item.UnitMasterInfo).WithMany()
				.HasForeignKey((IssueSalesItem item) => item.UnitMasterId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<PurchaseInvoice>().HasRequired((PurchaseInvoice document) => document.SupplierInfo).WithMany()
				.HasForeignKey((PurchaseInvoice document) => document.SupplierId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<PurchaseInvoice>().HasRequired((PurchaseInvoice document) => document.CurrencyInfo).WithMany()
				.HasForeignKey((PurchaseInvoice document) => document.CurrencyId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasRequired((ItemMaster item) => item.ItemCategoryInfo).WithMany()
				.HasForeignKey((ItemMaster item) => item.ItemCategoryId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasRequired((ItemMaster item) => item.UnitSetInfo).WithMany()
				.HasForeignKey((ItemMaster item) => item.UnitSetId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasRequired((ItemMaster item) => item.BaseUnit).WithMany()
				.HasForeignKey((ItemMaster item) => item.BaseUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasOptional((ItemMaster item) => item.PurchaseUnit).WithMany()
				.HasForeignKey((ItemMaster item) => item.PurchaseUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasOptional((ItemMaster item) => item.SalesUnit).WithMany()
				.HasForeignKey((ItemMaster item) => item.SalesUnitId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemMaster>().HasOptional((ItemMaster item) => item.DefaultSupplierInfo).WithMany()
				.HasForeignKey((ItemMaster item) => item.DefaultSupplierId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<ItemSubCategory>().HasRequired((ItemSubCategory item) => item.ItemCategoryInfo).WithMany((ItemCategory category) => category.SubCategories)
				.HasForeignKey((ItemSubCategory item) => item.ItemCategoryId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<Supplier>().HasOptional((Supplier supplier) => supplier.CountryInfo).WithMany()
				.HasForeignKey((Supplier supplier) => supplier.CountryId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<Supplier>().HasOptional((Supplier supplier) => supplier.CurrencyInfo).WithMany()
				.HasForeignKey((Supplier supplier) => supplier.CurrencyId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<StockBalance>().HasRequired((StockBalance balance) => balance.ItemMasterInfo).WithMany()
				.HasForeignKey((StockBalance balance) => balance.ItemMasterId)
				.WillCascadeOnDelete(value: false);
			modelBuilder.Entity<StockMovement>().HasRequired((StockMovement movement) => movement.ItemMasterInfo).WithMany()
				.HasForeignKey((StockMovement movement) => movement.ItemMasterId)
				.WillCascadeOnDelete(value: false);
			ConfigureUniqueIndexes(modelBuilder);
		}

		private static void ConfigureUniqueIndexes(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<StockBalance>().HasIndex((StockBalance e) => new { e.TenantId, e.ItemMasterId }).HasName("UX_StockBalances_TenantId_ItemMasterId")
				.IsUnique();
			modelBuilder.Entity<PurchaseInvoice>().HasIndex((PurchaseInvoice e) => new { e.TenantId, e.InvoiceNo }).HasName("UX_PurchaseInvoices_TenantId_InvoiceNo")
				.IsUnique();
			modelBuilder.Entity<IssueSale>().HasIndex((IssueSale e) => new { e.TenantId, e.DocumentNo }).HasName("UX_IssueSales_TenantId_DocumentNo")
				.IsUnique();
			modelBuilder.Entity<DocumentSequence>().HasIndex((DocumentSequence e) => new { e.TenantId, e.DocumentType }).HasName("UX_DocumentSequences_TenantId_DocumentType")
				.IsUnique();
			modelBuilder.Entity<Country>().HasIndex((Country e) => new { e.TenantId, e.Code }).HasName("UX_Countries_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<Currency>().HasIndex((Currency e) => new { e.TenantId, e.Code }).HasName("UX_Currencies_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<UnitSet>().HasIndex((UnitSet e) => new { e.TenantId, e.Code }).HasName("UX_UnitSets_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<UnitMaster>().HasIndex((UnitMaster e) => new { e.TenantId, e.Code }).HasName("UX_UnitMasters_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<ItemCategory>().HasIndex((ItemCategory e) => new { e.TenantId, e.Code }).HasName("UX_ItemCategories_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<ItemSubCategory>().HasIndex((ItemSubCategory e) => new { e.TenantId, e.Code }).HasName("UX_ItemSubCategories_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<Supplier>().HasIndex((Supplier e) => new { e.TenantId, e.Code }).HasName("UX_Suppliers_TenantId_Code")
				.IsUnique();
			modelBuilder.Entity<ItemMaster>().HasIndex((ItemMaster e) => new { e.TenantId, e.ItemCode }).HasName("UX_ItemMasters_TenantId_ItemCode")
				.IsUnique();
			modelBuilder.Entity<TenantRequest>().HasIndex((TenantRequest e) => e.RequestNo).HasName("UX_TenantRequests_RequestNo")
				.IsUnique();
			modelBuilder.Entity<TenantRequest>().HasIndex((TenantRequest e) => new { e.TenancyName, e.Status }).HasName("IX_TenantRequests_TenancyName_Status");
			modelBuilder.Entity<TenantRequest>().HasIndex((TenantRequest e) => new { e.AdminEmailAddress, e.Status }).HasName("IX_TenantRequests_AdminEmailAddress_Status");
		}
	}
}
