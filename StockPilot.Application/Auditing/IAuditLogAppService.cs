using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Auditing.Dto;
using StockPilot.Dto;

namespace StockPilot.Auditing
{
	public interface IAuditLogAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<AuditLogListDto>> GetAuditLogs(GetAuditLogsInput input);

		Task<FileDto> GetAuditLogsToExcel(GetAuditLogsInput input);
	}
}
