using System.Collections.Generic;
using System.Linq;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Dependency;
using StockPilot.Authorization.Permissions.Dto;

namespace StockPilot.Authorization.Permissions
{
	public class PermissionAppService : StockPilotAppServiceBase, IPermissionAppService, IApplicationService, ITransientDependency
	{
		public ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions()
		{
			IReadOnlyList<Permission> permissions = base.PermissionManager.GetAllPermissions();
			IEnumerable<Permission> enumerable = permissions.Where((Permission p) => p.Parent == null);
			List<FlatPermissionWithLevelDto> result = new List<FlatPermissionWithLevelDto>();
			foreach (Permission rootPermission in enumerable)
			{
				int level = 0;
				AddPermission(rootPermission, permissions, result, level);
			}
			return new ListResultDto<FlatPermissionWithLevelDto>
			{
				Items = result
			};
		}

		private void AddPermission(Permission permission, IReadOnlyList<Permission> allPermissions, List<FlatPermissionWithLevelDto> result, int level)
		{
			FlatPermissionWithLevelDto flatPermission = permission.MapTo<FlatPermissionWithLevelDto>();
			flatPermission.Level = level;
			result.Add(flatPermission);
			if (permission.Children == null)
			{
				return;
			}
			foreach (Permission childPermission in allPermissions.Where((Permission p) => p.Parent != null && p.Parent.Name == permission.Name).ToList())
			{
				AddPermission(childPermission, allPermissions, result, level + 1);
			}
		}
	}
}
