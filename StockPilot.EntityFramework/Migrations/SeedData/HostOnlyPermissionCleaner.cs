using StockPilot.EntityFramework;

namespace StockPilot.Migrations.SeedData
{
	public class HostOnlyPermissionCleaner
	{
		private readonly StockPilotDbContext _context;

		public HostOnlyPermissionCleaner(StockPilotDbContext context)
		{
			_context = context;
		}

		public void Run()
		{
			_context.Database.ExecuteSqlCommand("\nDELETE FROM [dbo].[AbpPermissions]\nWHERE [TenantId] IS NOT NULL\n  AND ([Name] = 'Pages.Tenants' OR [Name] LIKE 'Pages.Tenants.%'\n    OR [Name] = 'Pages.Editions' OR [Name] LIKE 'Pages.Editions.%'\n    OR [Name] LIKE 'Pages.Administration.Host.%');");
		}
	}
}
