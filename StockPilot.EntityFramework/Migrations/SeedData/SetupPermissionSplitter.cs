using StockPilot.EntityFramework;

namespace StockPilot.Migrations.SeedData
{
	public class SetupPermissionSplitter
	{
		private readonly StockPilotDbContext _context;

		public SetupPermissionSplitter(StockPilotDbContext context)
		{
			_context = context;
		}

		public void Run()
		{
			_context.Database.ExecuteSqlCommand("\nIF EXISTS (SELECT 1 FROM [dbo].[AbpPermissions] WHERE [Name] IN ('Pages.StockPilot.Setup.Create', 'Pages.StockPilot.Setup.Edit', 'Pages.StockPilot.Setup.Delete'))\n   OR NOT EXISTS (SELECT 1 FROM [dbo].[AbpPermissions] WHERE [Name] LIKE 'Pages.StockPilot.Setup.%')\nBEGIN\n    DECLARE @lists TABLE ([List] nvarchar(64));\n    INSERT INTO @lists VALUES ('Categories'), ('SubCategories'), ('UnitSets'), ('Units'), ('UnitConversions'), ('Currencies'), ('CurrencyConversions'), ('Countries');\n\n    -- Setup -> each list (view); Setup.Create / Edit / Delete -> each list's action.\n    ;WITH Mapping AS\n    (\n        SELECT 'Pages.StockPilot.Setup' AS [OldName], 'Pages.StockPilot.Setup.' + [List] AS [NewName] FROM @lists\n        UNION ALL SELECT 'Pages.StockPilot.Setup.Create', 'Pages.StockPilot.Setup.' + [List] + '.Create' FROM @lists\n        UNION ALL SELECT 'Pages.StockPilot.Setup.Edit', 'Pages.StockPilot.Setup.' + [List] + '.Edit' FROM @lists\n        UNION ALL SELECT 'Pages.StockPilot.Setup.Delete', 'Pages.StockPilot.Setup.' + [List] + '.Delete' FROM @lists\n    )\n    INSERT INTO [dbo].[AbpPermissions] ([TenantId], [Name], [IsGranted], [CreationTime], [CreatorUserId], [RoleId], [UserId], [Discriminator])\n    SELECT p.[TenantId], m.[NewName], p.[IsGranted], GETDATE(), NULL, p.[RoleId], p.[UserId], p.[Discriminator]\n    FROM [dbo].[AbpPermissions] p\n    INNER JOIN Mapping m ON m.[OldName] = p.[Name]\n    WHERE NOT EXISTS (\n        SELECT 1 FROM [dbo].[AbpPermissions] x\n        WHERE x.[Name] = m.[NewName] AND x.[Discriminator] = p.[Discriminator]\n          AND ISNULL(x.[RoleId], -1) = ISNULL(p.[RoleId], -1) AND ISNULL(x.[UserId], -1) = ISNULL(p.[UserId], -1)\n          AND ISNULL(x.[TenantId], -1) = ISNULL(p.[TenantId], -1));\nEND;\n\nDELETE FROM [dbo].[AbpPermissions]\nWHERE [Name] IN ('Pages.StockPilot.Setup.Create', 'Pages.StockPilot.Setup.Edit', 'Pages.StockPilot.Setup.Delete', 'Pages.Tenant.Inventory', 'Pages.Tenant.Inventory.Approve');");
		}
	}
}
