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
	public sealed class Host_TenantRequests_BusinessInfo : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Host_TenantRequests_BusinessInfo));

		string IMigrationMetadata.Id => "202611081453189_Host_TenantRequests_BusinessInfo";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			CreateTable("dbo.TenantRequests", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel requestNo = c.String(false, 32);
				ColumnModel tenancyName = c.String(false, 64);
				ColumnModel storeName = c.String(false, 128);
				int? maxLength = 128;
				ColumnModel contactName = c.String(null, maxLength);
				maxLength = 256;
				ColumnModel contactEmail = c.String(null, maxLength);
				maxLength = 32;
				ColumnModel contactPhone = c.String(null, maxLength);
				maxLength = 512;
				ColumnModel address = c.String(null, maxLength);
				maxLength = 128;
				ColumnModel city = c.String(null, maxLength);
				maxLength = 128;
				ColumnModel countryName = c.String(null, maxLength);
				maxLength = 64;
				ColumnModel businessType = c.String(null, maxLength);
				maxLength = 2000;
				ColumnModel notes = c.String(null, maxLength);
				ColumnModel adminName = c.String(false, 32);
				ColumnModel adminSurname = c.String(false, 32);
				ColumnModel adminEmailAddress = c.String(false, 256);
				maxLength = 24;
				ColumnModel adminPhoneNumber = c.String(null, maxLength);
				maxLength = 128;
				ColumnModel adminPasswordHash = c.String(null, maxLength);
				ColumnModel requestedEditionId = c.Int();
				ColumnModel status = c.Int(false);
				ColumnModel reviewedByUserId = c.Long();
				ColumnModel reviewedTime = c.DateTime();
				maxLength = 1000;
				ColumnModel reviewNote = c.String(null, maxLength);
				ColumnModel createdTenantId = c.Int();
				maxLength = 64;
				return new
				{
					Id = id,
					RequestNo = requestNo,
					TenancyName = tenancyName,
					StoreName = storeName,
					ContactName = contactName,
					ContactEmail = contactEmail,
					ContactPhone = contactPhone,
					Address = address,
					City = city,
					CountryName = countryName,
					BusinessType = businessType,
					Notes = notes,
					AdminName = adminName,
					AdminSurname = adminSurname,
					AdminEmailAddress = adminEmailAddress,
					AdminPhoneNumber = adminPhoneNumber,
					AdminPasswordHash = adminPasswordHash,
					RequestedEditionId = requestedEditionId,
					Status = status,
					ReviewedByUserId = reviewedByUserId,
					ReviewedTime = reviewedTime,
					ReviewNote = reviewNote,
					CreatedTenantId = createdTenantId,
					RequesterIpAddress = c.String(null, maxLength),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object> { { "DynamicFilter_TenantRequest_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).Index(t => t.RequestNo, "UX_TenantRequests_RequestNo", unique: true).Index(t => new { t.TenancyName, t.Status }, "IX_TenantRequests_TenancyName_Status")
				.Index(t => new { t.AdminEmailAddress, t.Status }, "IX_TenantRequests_AdminEmailAddress_Status");
			AddColumn("dbo.AbpTenants", "ContactName", delegate(ColumnBuilder c)
			{
				int? maxLength = 128;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "ContactEmail", delegate(ColumnBuilder c)
			{
				int? maxLength = 256;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "ContactPhone", delegate(ColumnBuilder c)
			{
				int? maxLength = 32;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "Address", delegate(ColumnBuilder c)
			{
				int? maxLength = 512;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "City", delegate(ColumnBuilder c)
			{
				int? maxLength = 128;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "CountryName", delegate(ColumnBuilder c)
			{
				int? maxLength = 128;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "BusinessType", delegate(ColumnBuilder c)
			{
				int? maxLength = 64;
				return c.String(null, maxLength);
			});
			AddColumn("dbo.AbpTenants", "Notes", delegate(ColumnBuilder c)
			{
				int? maxLength = 2000;
				return c.String(null, maxLength);
			});
		}

		public override void Down()
		{
			DropIndex("dbo.TenantRequests", "IX_TenantRequests_AdminEmailAddress_Status");
			DropIndex("dbo.TenantRequests", "IX_TenantRequests_TenancyName_Status");
			DropIndex("dbo.TenantRequests", "UX_TenantRequests_RequestNo");
			DropColumn("dbo.AbpTenants", "Notes");
			DropColumn("dbo.AbpTenants", "BusinessType");
			DropColumn("dbo.AbpTenants", "CountryName");
			DropColumn("dbo.AbpTenants", "City");
			DropColumn("dbo.AbpTenants", "Address");
			DropColumn("dbo.AbpTenants", "ContactPhone");
			DropColumn("dbo.AbpTenants", "ContactEmail");
			DropColumn("dbo.AbpTenants", "ContactName");
			DropTable("dbo.TenantRequests", new Dictionary<string, object> { { "DynamicFilter_TenantRequest_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
		}
	}
}
