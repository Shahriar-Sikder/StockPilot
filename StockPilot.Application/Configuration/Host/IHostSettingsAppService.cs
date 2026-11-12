using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Configuration.Host.Dto;

namespace StockPilot.Configuration.Host
{
	public interface IHostSettingsAppService : IApplicationService, ITransientDependency
	{
		Task<HostSettingsEditDto> GetAllSettings();

		Task UpdateAllSettings(HostSettingsEditDto input);

		Task SendTestEmail(SendTestEmailInput input);
	}
}
