using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Configuration.Tenants.Dto;

namespace StockPilot.Configuration.Tenants
{
	public interface ITenantSettingsAppService : IApplicationService, ITransientDependency
	{
		Task<TenantSettingsEditDto> GetAllSettings();

		Task UpdateAllSettings(TenantSettingsEditDto input);

		Task ClearLogo();

		Task ClearCustomCss();
	}
}
