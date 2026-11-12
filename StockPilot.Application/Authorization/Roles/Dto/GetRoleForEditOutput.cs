using System.Collections.Generic;
using StockPilot.Authorization.Permissions.Dto;

namespace StockPilot.Authorization.Roles.Dto
{
	public class GetRoleForEditOutput
	{
		public RoleEditDto Role { get; set; }

		public List<FlatPermissionDto> Permissions { get; set; }

		public List<string> GrantedPermissionNames { get; set; }

		public List<string> GrantablePermissionNames { get; set; }
	}
}
