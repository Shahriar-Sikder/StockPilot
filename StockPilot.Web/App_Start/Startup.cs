using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.Owin;
using Abp.Timing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Twitter;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using StockPilot.Web.Auth;
using StockPilot.WebApi.Controllers;

namespace StockPilot.Web
{
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseAbp();
			app.RegisterDataProtectionProvider();
			app.UseOAuthBearerAuthentication(AccountController.OAuthBearerOptions);
			app.UseCookieAuthentication(new CookieAuthenticationOptions
			{
				AuthenticationType = "ApplicationCookie",
				ExpireTimeSpan = TimeSpan.FromMinutes(360.0),
				LoginPath = new PathString("/Account/Login")
			});
			app.UseExternalSignInCookie("ExternalCookie");
			app.UseTwoFactorSignInCookie("TwoFactorCookie", TimeSpan.FromMinutes(5.0));
			app.UseTwoFactorRememberBrowserCookie("TwoFactorRememberBrowser");
			if (IsTrue("ExternalAuth.Facebook.IsEnabled"))
			{
				app.UseFacebookAuthentication(CreateFacebookAuthOptions());
			}
			if (IsTrue("ExternalAuth.Twitter.IsEnabled"))
			{
				app.UseTwitterAuthentication(CreateTwitterAuthOptions());
			}
			if (IsTrue("ExternalAuth.Google.IsEnabled"))
			{
				app.UseGoogleAuthentication(CreateGoogleAuthOptions());
			}
			if (IsTrue("ExternalAuth.WsFederation.IsEnabled"))
			{
				app.UseWsFederationAuthentication(CreateWsFederationAuthOptions());
			}
			if (IsTrue("ExternalAuth.OpenId.IsEnabled"))
			{
				app.UseOpenIdConnectAuthentication(CreateOpenIdOptions());
			}
			Clock.Provider = ClockProviders.Unspecified;
			app.MapSignalR();
		}

		private static OpenIdConnectAuthenticationOptions CreateOpenIdOptions()
		{
			OpenIdConnectAuthenticationOptions options = new OpenIdConnectAuthenticationOptions
			{
				Authority = ConfigurationManager.AppSettings["ExternalAuth.OpenId.Authority"],
				ClientId = ConfigurationManager.AppSettings["ExternalAuth.OpenId.ClientId"],
				PostLogoutRedirectUri = WebUrlService.WebSiteRootAddress + "Account/Logout",
				Notifications = new OpenIdConnectAuthenticationNotifications
				{
					SecurityTokenValidated = delegate(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
					{
						string name = notification.AuthenticationTicket.Identity.Name;
						notification.AuthenticationTicket.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", name));
						return Task.FromResult(0);
					}
				}
			};
			string clientSecret = ConfigurationManager.AppSettings["ExternalAuth.OpenId.ClientSecret"];
			if (!clientSecret.IsNullOrEmpty())
			{
				options.ClientSecret = clientSecret;
			}
			return options;
		}

		private static FacebookAuthenticationOptions CreateFacebookAuthOptions()
		{
			return new FacebookAuthenticationOptions
			{
				AppId = ConfigurationManager.AppSettings["ExternalAuth.Facebook.AppId"],
				AppSecret = ConfigurationManager.AppSettings["ExternalAuth.Facebook.AppSecret"],
				Scope = { "email", "public_profile" }
			};
		}

		private static TwitterAuthenticationOptions CreateTwitterAuthOptions()
		{
			string consumerKey = ConfigurationManager.AppSettings["ExternalAuth.Twitter.ConsumerKey"];
			string consumerSecret = ConfigurationManager.AppSettings["ExternalAuth.Twitter.ConsumerSecret"];
			TwitterAuthenticationOptions twitterAuthenticationOptions = new TwitterAuthenticationOptions();
			twitterAuthenticationOptions.ConsumerKey = consumerKey;
			twitterAuthenticationOptions.ConsumerSecret = consumerSecret;
			twitterAuthenticationOptions.Provider = new TwitterAuthenticationProvider
			{
				OnAuthenticated = delegate(TwitterAuthenticatedContext context)
				{
					context.Identity.AddClaim(new Claim("urn:twitter:access_token", context.AccessToken));
					context.Identity.AddClaim(new Claim("urn:twitter:access_secret", context.AccessTokenSecret));
					string value = new TwitterEmailRetriever().Get(context.AccessToken, context.AccessTokenSecret, consumerKey, consumerSecret);
					context.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", value));
					return Task.FromResult(0);
				}
			};
			twitterAuthenticationOptions.BackchannelCertificateValidator = new CertificateSubjectKeyIdentifierValidator(new string[8] { "A5EF0B11CEC04103A34A659048B21CE0572D7D47", "0D445C165344C1827E1D20AB25F40163D8BE79A5", "7FD365A7C2DDECBBF03009F34339FA02AF333133", "39A55D933676616E73A761DFA16A7E59CDE66FAD", "\u200eadd53f6680fe66e383cbac3e60922e3b4c412bed", "4eb6d578499b1ccf5f581ead56be3d9b6744a5e5", "5168FF90AF0207753CCCD9656462A212B859723B", "B13EC36903F8BF4701D498261A0802EF63642BC3" });
			return twitterAuthenticationOptions;
		}

		private static GoogleOAuth2AuthenticationOptions CreateGoogleAuthOptions()
		{
			return new GoogleOAuth2AuthenticationOptions
			{
				ClientId = ConfigurationManager.AppSettings["ExternalAuth.Google.ClientId"],
				ClientSecret = ConfigurationManager.AppSettings["ExternalAuth.Google.ClientSecret"]
			};
		}

		private static WsFederationAuthenticationOptions CreateWsFederationAuthOptions()
		{
			string wtrealm = ConfigurationManager.AppSettings["ExternalAuth.WsFederation.Wtrealm"];
			string metaDataAddress = ConfigurationManager.AppSettings["ExternalAuth.WsFederation.MetaDataAddress"];
			return new WsFederationAuthenticationOptions
			{
				Wtrealm = wtrealm,
				MetadataAddress = metaDataAddress,
				AuthenticationType = "adfs",
				Notifications = new WsFederationAuthenticationNotifications
				{
					RedirectToIdentityProvider = delegate(RedirectToIdentityProviderNotification<WsFederationMessage, WsFederationAuthenticationOptions> notification)
					{
						if (notification.ProtocolMessage.IsSignOutMessage)
						{
							notification.HandleResponse();
						}
						notification.ProtocolMessage.Wreply = WebUrlService.WebSiteRootAddress + "Account/ExternalLoginCallback";
						return Task.FromResult(0);
					},
					SecurityTokenValidated = delegate(SecurityTokenValidatedNotification<WsFederationMessage, WsFederationAuthenticationOptions> notification)
					{
						Claim claim = notification.AuthenticationTicket.Identity.Claims.ToList().FirstOrDefault((Claim c) => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
						if (claim != null)
						{
							notification.AuthenticationTicket.Identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", claim.Value));
						}
						return Task.FromResult(0);
					}
				}
			};
		}

		private static bool IsTrue(string appSettingName)
		{
			return string.Equals(ConfigurationManager.AppSettings[appSettingName], "true", StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
