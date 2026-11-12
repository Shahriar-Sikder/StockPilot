using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Services.HostDashboard.Dtos;

namespace StockPilot.Services.HostDashboard
{
	public interface IHostDashboardAppService : IApplicationService, ITransientDependency
	{
		Task<HostDashboardDto> GetSummary();
	}
}
