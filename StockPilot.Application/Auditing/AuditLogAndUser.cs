using Abp.Auditing;
using StockPilot.Authorization.Users;

namespace StockPilot.Auditing
{
	public class AuditLogAndUser
	{
		public AuditLog AuditLog { get; set; }

		public User User { get; set; }
	}
}
