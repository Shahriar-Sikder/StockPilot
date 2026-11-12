using System.Data.Entity.Migrations;
using System.Linq;
using Abp.Configuration;
using Abp.Events.Bus;
using Abp.Events.Bus.Entities;
using Abp.Localization;
using EntityFramework.DynamicFilters;
using StockPilot.EntityFramework;
using StockPilot.Migrations.SeedData;

namespace StockPilot.Migrations
{
	public sealed class Configuration : DbMigrationsConfiguration<StockPilotDbContext>
	{
		public Configuration()
		{
			base.AutomaticMigrationsEnabled = true;
			base.AutomaticMigrationDataLossAllowed = false;
			base.ContextKey = "StockPilot.EntityFramework.StockPilotDbContext";
		}

		protected override void Seed(StockPilotDbContext context)
		{
			context.EntityChangeEventHelper = NullEntityChangeEventHelper.Instance;
			context.EventBus = NullEventBus.Instance;
			context.DisableAllFilters();
			if (!context.Languages.Any((ApplicationLanguage language) => language.TenantId == (int?)null && language.Name == "en"))
			{
				context.Languages.Add(new ApplicationLanguage(null, "en", "English", "famfamfam-flag-gb"));
			}
			if (!context.Languages.Any((ApplicationLanguage language) => language.TenantId == (int?)null && language.Name == "bn"))
			{
				context.Languages.Add(new ApplicationLanguage(null, "bn", "ব\u09be\u0982ল\u09be", "famfamfam-flag-bd"));
			}
			if (!context.Languages.Any((ApplicationLanguage language) => language.TenantId == (int?)null && language.Name == "hi"))
			{
				context.Languages.Add(new ApplicationLanguage(null, "hi", "ह\u093fन\u094dद\u0940", "famfamfam-flag-in"));
			}
			if (!context.Settings.Any((Setting setting) => setting.TenantId == (int?)null && setting.UserId == (long?)null && setting.Name == "Abp.Localization.DefaultLanguageName"))
			{
				context.Settings.Add(new Setting(null, null, "Abp.Localization.DefaultLanguageName", "en"));
			}
			context.SaveChanges();
			context.Database.ExecuteSqlCommand("\n;WITH RankedLanguages AS\n(\n    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [TenantId], [Name] ORDER BY [Id]) AS [RowNumber]\n    FROM [dbo].[AbpLanguages]\n)\nDELETE FROM RankedLanguages WHERE [RowNumber] > 1;\n\nDELETE tenantLanguage\nFROM [dbo].[AbpLanguages] tenantLanguage\nINNER JOIN [dbo].[AbpLanguages] hostLanguage\n    ON hostLanguage.[TenantId] IS NULL AND hostLanguage.[Name] = tenantLanguage.[Name]\nWHERE tenantLanguage.[TenantId] IS NOT NULL;");
			new HostRoleAndUserCreator(context).Create();
			new SetupPermissionSplitter(context).Run();
			new HostOnlyPermissionCleaner(context).Run();
		}
	}
}
