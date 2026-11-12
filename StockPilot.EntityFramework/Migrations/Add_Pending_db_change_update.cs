using System.CodeDom.Compiler;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Infrastructure;
using System.Resources;

namespace StockPilot.Migrations
{
	[GeneratedCode("EntityFramework.Migrations", "6.4.4")]
	public sealed class Add_Pending_db_change_update : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Add_Pending_db_change_update));

		string IMigrationMetadata.Id => "202611051842256_Add_Pending_db_change_update";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			AlterColumn("dbo.IssueSales", "DocumentNo", (ColumnBuilder c) => c.String(false, 50));
		}

		public override void Down()
		{
			AlterColumn("dbo.IssueSales", "DocumentNo", (ColumnBuilder c) => c.String(false));
		}
	}
}
