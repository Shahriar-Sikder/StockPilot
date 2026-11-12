using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Authorization;
using StockPilot.Authorization.Permissions.Dto;

namespace StockPilot.Authorization.Permissions
{
	public static class PermissionFeatureFilter
	{
		public static async Task<List<FlatPermissionDto>> RemoveDisabledFeaturePermissions(List<FlatPermissionDto> permissions, IEnumerable<string> grantedPermissionNames, IFeatureChecker featureChecker, bool isTenant, IPermissionManager permissionManager)
		{
			if (!isTenant)
			{
				return permissions;
			}
			HashSet<string> hidden = new HashSet<string>();
			foreach (FlatPermissionDto dto in permissions)
			{
				SimpleFeatureDependency dependency = permissionManager.GetPermissionOrNull(dto.Name)?.FeatureDependency as SimpleFeatureDependency;
				bool flag = dependency != null;
				if (flag)
				{
					flag = !(await IsSatisfiedAsync(dependency, featureChecker));
				}
				if (flag)
				{
					hidden.Add(dto.Name);
				}
			}
			bool added;
			do
			{
				added = false;
				foreach (FlatPermissionDto dto2 in permissions.Where((FlatPermissionDto p) => !hidden.Contains(p.Name) && p.ParentName != null && hidden.Contains(p.ParentName)))
				{
					hidden.Add(dto2.Name);
					added = true;
				}
			}
			while (added);
			return permissions.Where((FlatPermissionDto p) => !hidden.Contains(p.Name)).ToList();
		}

		private static async Task<bool> IsSatisfiedAsync(SimpleFeatureDependency dependency, IFeatureChecker featureChecker)
		{
			string[] features = dependency.Features;
			foreach (string feature in features)
			{
				bool enabled = await featureChecker.IsEnabledAsync(feature);
				if (dependency.RequiresAll && !enabled)
				{
					return false;
				}
				if (!dependency.RequiresAll && enabled)
				{
					return true;
				}
			}
			return dependency.RequiresAll;
		}
	}
}
