using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace StockPilot.Web.Controllers.Results
{
	public class ChallengeResult : HttpUnauthorizedResult
	{
		public string LoginProvider { get; set; }

		public string RedirectUri { get; set; }

		public string UserId { get; set; }

		public ChallengeResult(string provider, string redirectUri)
			: this(provider, redirectUri, null)
		{
		}

		public ChallengeResult(string provider, string redirectUri, string userId)
		{
			LoginProvider = provider;
			RedirectUri = redirectUri;
			UserId = userId;
		}

		public override void ExecuteResult(ControllerContext context)
		{
			AuthenticationProperties properties = new AuthenticationProperties
			{
				RedirectUri = RedirectUri
			};
			if (UserId != null)
			{
				properties.Dictionary["XsrfId"] = UserId;
			}
			context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
		}
	}
}
