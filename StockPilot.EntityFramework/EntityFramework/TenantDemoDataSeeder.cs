using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFramework;
using StockPilot.MultiTenancy;

namespace StockPilot.EntityFramework
{
	public class TenantDemoDataSeeder : ITenantDemoDataSeeder, ITransientDependency
	{
		private readonly IDbContextProvider<StockPilotDbContext> _dbContextProvider;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		public TenantDemoDataSeeder(IDbContextProvider<StockPilotDbContext> dbContextProvider, IUnitOfWorkManager unitOfWorkManager)
		{
			_dbContextProvider = dbContextProvider;
			_unitOfWorkManager = unitOfWorkManager;
		}

		public void SeedIfEnabled(Tenant tenant)
		{
			if (!StockPilotDemoSeeder.IsEnabledFor(tenant.Name))
			{
				return;
			}
			using (_unitOfWorkManager.Current.SetTenantId(tenant.Id))
			{
				StockPilotDemoSeeder.Seed(_dbContextProvider.GetDbContext(), tenant.Id);
			}
		}
	}
}
