using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Authorization;
using Abp.Runtime.Validation;

namespace StockPilot.Authorization.Permissions
{
	public static class PermissionManagerExtensions
	{
		public static IEnumerable<Permission> GetPermissionsFromNamesByValidating(this IPermissionManager permissionManager, IEnumerable<string> permissionNames)
		{
			List<Permission> permissions = new List<Permission>();
			List<string> undefinedPermissionNames = new List<string>();
			foreach (string permissionName in permissionNames)
			{
				Permission permission = permissionManager.GetPermissionOrNull(permissionName);
				if (permission == null)
				{
					undefinedPermissionNames.Add(permissionName);
				}
				permissions.Add(permission);
			}
			if (undefinedPermissionNames.Count > 0)
			{
				throw new AbpValidationException($"There are {undefinedPermissionNames.Count} undefined permission names.")
				{
					ValidationErrors = undefinedPermissionNames.ConvertAll((string text) => new ValidationResult("Undefined permission: " + text))
				};
			}
			return permissions;
		}
	}
}
