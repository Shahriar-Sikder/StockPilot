using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;

namespace StockPilot.Authorization
{
	public interface IWorkspaceAppService : IApplicationService, ITransientDependency
	{
		Task<WorkspaceInfoDto> GetWorkspaceInfo();
	}
}
