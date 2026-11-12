using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Services.TenantRequest.Dtos;

namespace StockPilot.Services.TenantRequest
{
	public interface ITenantRequestAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<TenantRequestListDto>> GetAll(TenantRequestGridInput input);

		Task<TenantRequestCountsDto> GetCounts();

		Task<EditTenantRequestDto> GetForEdit(EntityDto input);

		Task Update(EditTenantRequestDto input);

		Task Approve(ApproveTenantRequestDto input);

		Task Reject(RejectTenantRequestDto input);

		Task Delete(EntityDto input);
	}
}
