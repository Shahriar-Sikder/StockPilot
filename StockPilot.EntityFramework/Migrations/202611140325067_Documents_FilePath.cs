namespace StockPilot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Documents_FilePath : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.PurchaseInvoiceDocuments", new[] { "BinaryObjectId" });
            AddColumn("dbo.PurchaseInvoiceDocuments", "FilePath", c => c.String(maxLength: 500));
            AlterColumn("dbo.PurchaseInvoiceDocuments", "BinaryObjectId", c => c.Guid());
            CreateIndex("dbo.PurchaseInvoiceDocuments", "BinaryObjectId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.PurchaseInvoiceDocuments", new[] { "BinaryObjectId" });
            AlterColumn("dbo.PurchaseInvoiceDocuments", "BinaryObjectId", c => c.Guid(nullable: false));
            DropColumn("dbo.PurchaseInvoiceDocuments", "FilePath");
            CreateIndex("dbo.PurchaseInvoiceDocuments", "BinaryObjectId");
        }
    }
}
