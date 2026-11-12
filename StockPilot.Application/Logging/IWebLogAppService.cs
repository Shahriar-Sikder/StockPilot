using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Logging.Dto;

namespace StockPilot.Logging
{
	public interface IWebLogAppService : IApplicationService, ITransientDependency
	{
		GetLatestWebLogsOutput GetLatestWebLogs();

		FileDto DownloadWebLogs();
	}
}
