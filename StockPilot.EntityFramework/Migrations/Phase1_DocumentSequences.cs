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
	public sealed class Phase1_DocumentSequences : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Phase1_DocumentSequences));

		string IMigrationMetadata.Id => "202611070746191_Phase1_DocumentSequences";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			CreateTable("dbo.DocumentSequences", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel documentType = c.String(false, 10);
				ColumnModel nextNumber = c.Int(false);
				ColumnModel lastModificationTime = c.DateTime();
				bool? nullable = false;
				bool? fixedLength = true;
				return new
				{
					Id = id,
					TenantId = tenantId,
					DocumentType = documentType,
					NextNumber = nextNumber,
					LastModificationTime = lastModificationTime,
					RowVersion = c.Binary(nullable, null, fixedLength, null, null, timestamp: true, null, "rowversion")
				};
			}, new Dictionary<string, object> { { "DynamicFilter_DocumentSequence_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).Index(t => new { t.TenantId, t.DocumentType }, "UX_DocumentSequences_TenantId_DocumentType", unique: true);
			AlterColumn("dbo.IssueSalesItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
			AlterColumn("dbo.PurchaseInvoiceItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)6));
		}

		public override void Down()
		{
			DropIndex("dbo.DocumentSequences", "UX_DocumentSequences_TenantId_DocumentType");
			AlterColumn("dbo.PurchaseInvoiceItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			AlterColumn("dbo.IssueSalesItems", "ConversionFactor", (ColumnBuilder c) => c.Decimal(false, (byte)18, (byte)4));
			DropTable("dbo.DocumentSequences", new Dictionary<string, object> { { "DynamicFilter_DocumentSequence_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
		}
	}
}
