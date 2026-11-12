using System.Collections.Generic;
using StockPilot.Authorization.Permissions.Dto;

namespace StockPilot.Authorization.Users.Dto
{
	public class GetUserPermissionsForEditOutput
	{
		public List<FlatPermissionDto> Permissions { get; set; }

		public List<string> GrantedPermissionNames { get; set; }

		public List<string> RolePermissionNames { get; set; }

		public List<string> GrantablePermissionNames { get; set; }

		public bool IsOwnAccount { get; set; }
	}
}
