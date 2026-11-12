using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.MultiTenancy.Dto;

namespace StockPilot.MultiTenancy
{
	public interface ITenantAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<TenantListDto>> GetTenants(GetTenantsInput input);

		Task CreateTenant(CreateTenantInput input);

		Task<TenantEditDto> GetTenantForEdit(EntityDto input);

		Task UpdateTenant(TenantEditDto input);

		Task DeleteTenant(EntityDto input);

		Task<TenantFeaturesForEditOutput> GetTenantFeaturesForEdit(EntityDto input);

		Task UpdateTenantFeatures(UpdateTenantFeaturesInput input);

		Task ResetTenantSpecificFeatures(EntityDto input);

		Task UnlockTenantAdmin(EntityDto input);

		Task SetActive(SetTenantActiveInput input);

		Task ExtendSubscription(ExtendSubscriptionInput input);

		Task<TenantFeatureUsageDto> GetFeatureUsage(EntityDto input);
	}
}
