using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Authorization.Roles.Dto;

namespace StockPilot.Authorization.Roles
{
	public interface IRoleAppService : IApplicationService, ITransientDependency
	{
		Task<ListResultDto<RoleListDto>> GetRoles(GetRolesInput input);

		Task<GetRoleForEditOutput> GetRoleForEdit(NullableIdDto input);

		Task CreateOrUpdateRole(CreateOrUpdateRoleInput input);

		Task DeleteRole(EntityDto input);
	}
}
