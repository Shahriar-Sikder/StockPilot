using Abp.Authorization.Roles;
using StockPilot.Authorization.Users;

namespace StockPilot.Authorization.Roles
{
	public class Role : AbpRole<User>
	{
		public Role()
		{
		}

		public Role(int? tenantId, string displayName)
			: base(tenantId, displayName)
		{
		}

		public Role(int? tenantId, string name, string displayName)
			: base(tenantId, name, displayName)
		{
		}
	}
}
