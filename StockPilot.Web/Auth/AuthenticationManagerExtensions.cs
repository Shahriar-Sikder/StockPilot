using System.Security.Claims;
using Microsoft.Owin.Security;

namespace StockPilot.Web.Auth
{
	public static class AuthenticationManagerExtensions
	{
		public static void SignOutAll(this IAuthenticationManager authenticationManager)
		{
			authenticationManager.SignOut("ApplicationCookie", "ExternalCookie", "TwoFactorCookie");
		}

		public static void SignOutAllAndSignIn(this IAuthenticationManager authenticationManager, ClaimsIdentity identity, bool rememberMe = false)
		{
			authenticationManager.SignOutAll();
			authenticationManager.SignIn(new AuthenticationProperties
			{
				IsPersistent = rememberMe
			}, identity);
		}
	}
}
