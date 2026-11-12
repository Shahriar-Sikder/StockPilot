using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Runtime.Session;

namespace StockPilot.Authorization
{
	[AbpAuthorize(new string[] { })]
	public class WorkspaceAppService : StockPilotAppServiceBase, IWorkspaceAppService, IApplicationService, ITransientDependency
	{
		private readonly IPermissionManager _permissionManager;

		private readonly IPermissionChecker _permissionChecker;

		private readonly IAbpSession _session;

		public WorkspaceAppService(IPermissionManager permissionManager, IPermissionChecker permissionChecker, IAbpSession session)
		{
			_permissionManager = permissionManager;
			_permissionChecker = permissionChecker;
			_session = session;
		}

		public Task<WorkspaceInfoDto> GetWorkspaceInfo()
		{
			string[] permissions = (from permission in _permissionManager.GetAllPermissions()
				where _permissionChecker.IsGranted(permission.Name)
				select permission.Name).ToArray();
			return Task.FromResult(new WorkspaceInfoDto
			{
				TenantId = _session.TenantId,
				UserId = _session.UserId,
				Permissions = permissions
			});
		}
	}
}
