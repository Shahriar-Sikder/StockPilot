using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Sessions.Dto;

namespace StockPilot.Sessions
{
	public interface ISessionAppService : IApplicationService, ITransientDependency
	{
		Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
	}
}
