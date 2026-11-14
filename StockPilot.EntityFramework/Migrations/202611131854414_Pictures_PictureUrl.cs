namespace StockPilot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Pictures_PictureUrl : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItemMasters", "PictureUrl", c => c.String(maxLength: 500));
            AddColumn("dbo.ItemCategories", "PictureUrl", c => c.String(maxLength: 500));
            AddColumn("dbo.ItemSubCategories", "PictureUrl", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItemSubCategories", "PictureUrl");
            DropColumn("dbo.ItemCategories", "PictureUrl");
            DropColumn("dbo.ItemMasters", "PictureUrl");
        }
    }
}
