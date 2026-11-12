using System.CodeDom.Compiler;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Infrastructure;
using System.Resources;

namespace StockPilot.Migrations
{
	[GeneratedCode("EntityFramework.Migrations", "6.4.4")]
	public sealed class Phase0_UnitSets_UniqueIndexes : DbMigration, IMigrationMetadata
	{
		private static readonly string[][] MasterCodeIndexes = new string[8][]
		{
			new string[2] { "Countries", "Code" },
			new string[2] { "Currencies", "Code" },
			new string[2] { "UnitSets", "Code" },
			new string[2] { "UnitMasters", "Code" },
			new string[2] { "ItemCategories", "Code" },
			new string[2] { "ItemSubCategories", "Code" },
			new string[2] { "Suppliers", "Code" },
			new string[2] { "ItemMasters", "ItemCode" }
		};

		private readonly ResourceManager Resources = new ResourceManager(typeof(Phase0_UnitSets_UniqueIndexes));

		string IMigrationMetadata.Id => "202611070737170_Phase0_UnitSets_UniqueIndexes";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			DropForeignKey("dbo.UnitMasters", "UnitSet_Id", "dbo.UnitSets");
			DropIndex("dbo.UnitMasters", new string[1] { "UnitSet_Id" });
			DropColumn("dbo.UnitMasters", "UnitSet_Id");
			DropForeignKey("dbo.UnitConversions", "UnitSetId", "dbo.UnitSets");
			AddForeignKey("dbo.UnitConversions", "UnitSetId", "dbo.UnitSets", "Id");
			DropIndex("dbo.StockBalances", new string[1] { "ItemMasterId" });
			CreateIndex("dbo.StockBalances", new string[2] { "TenantId", "ItemMasterId" }, unique: true, "UX_StockBalances_TenantId_ItemMasterId");
			CreateIndex("dbo.PurchaseInvoices", new string[2] { "TenantId", "InvoiceNo" }, unique: true, "UX_PurchaseInvoices_TenantId_InvoiceNo");
			CreateIndex("dbo.IssueSales", new string[2] { "TenantId", "DocumentNo" }, unique: true, "UX_IssueSales_TenantId_DocumentNo");
			string[][] masterCodeIndexes = MasterCodeIndexes;
			foreach (string[] index in masterCodeIndexes)
			{
				Sql(string.Format("CREATE UNIQUE NONCLUSTERED INDEX [UX_{0}_TenantId_{1}] ON [dbo].[{0}] ([TenantId], [{1}]) WHERE [IsDeleted] = 0", index[0], index[1]));
			}
		}

		public override void Down()
		{
			string[][] masterCodeIndexes = MasterCodeIndexes;
			foreach (string[] index in masterCodeIndexes)
			{
				DropIndex("dbo." + index[0], "UX_" + index[0] + "_TenantId_" + index[1]);
			}
			DropIndex("dbo.IssueSales", "UX_IssueSales_TenantId_DocumentNo");
			DropIndex("dbo.PurchaseInvoices", "UX_PurchaseInvoices_TenantId_InvoiceNo");
			DropIndex("dbo.StockBalances", "UX_StockBalances_TenantId_ItemMasterId");
			CreateIndex("dbo.StockBalances", "ItemMasterId");
			DropForeignKey("dbo.UnitConversions", "UnitSetId", "dbo.UnitSets");
			AddForeignKey("dbo.UnitConversions", "UnitSetId", "dbo.UnitSets", "Id", cascadeDelete: true);
			AddColumn("dbo.UnitMasters", "UnitSet_Id", (ColumnBuilder c) => c.Int());
			CreateIndex("dbo.UnitMasters", "UnitSet_Id");
			AddForeignKey("dbo.UnitMasters", "UnitSet_Id", "dbo.UnitSets", "Id");
		}
	}
}
