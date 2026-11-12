using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Services.Dashboard.Dtos;

namespace StockPilot.Services.Dashboard
{
	public interface IDashboardAppService : IApplicationService, ITransientDependency
	{
		Task<DashboardSummaryDto> GetSummary();

		Task<QuantityDashboardDto> GetQuantitySummary();
	}
}
