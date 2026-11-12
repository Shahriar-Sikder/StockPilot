namespace StockPilot.MultiTenancy
{
	public interface ITenantDemoDataSeeder
	{
		void SeedIfEnabled(Tenant tenant);
	}
}
