using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot.Authorization.Ldap
{
	public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
	{
		public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
			: base(settings, ldapModuleConfig)
		{
		}
	}
}
