using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Authorization.Permissions.Dto;

namespace StockPilot.Authorization.Permissions
{
	public interface IPermissionAppService : IApplicationService, ITransientDependency
	{
		ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
	}
}
