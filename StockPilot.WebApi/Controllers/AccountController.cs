using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Uow;
using Abp.Web.Models;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using StockPilot.Authorization;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;
using StockPilot.WebApi.Models;

namespace StockPilot.WebApi.Controllers
{
	public class AccountController : StockPilotApiControllerBase
	{
		private readonly LogInManager _logInManager;

		private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;

		private readonly UserManager _userManager;

		private readonly TenantManager _tenantManager;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		public static OAuthBearerAuthenticationOptions OAuthBearerOptions { get; private set; }

		public HostLoginPolicy HostLoginPolicy { get; set; }

		static AccountController()
		{
			OAuthBearerOptions = new OAuthBearerAuthenticationOptions();
		}

		public AccountController(AbpLoginResultTypeHelper abpLoginResultTypeHelper, LogInManager logInManager, UserManager userManager, TenantManager tenantManager, IUnitOfWorkManager unitOfWorkManager)
		{
			_abpLoginResultTypeHelper = abpLoginResultTypeHelper;
			_logInManager = logInManager;
			_userManager = userManager;
			_tenantManager = tenantManager;
			_unitOfWorkManager = unitOfWorkManager;
		}

		[HttpPost]
		[UnitOfWork]
		public async Task<AjaxResponse> Authenticate(LoginModel loginModel)
		{
			await HostLoginPolicy.CheckAsync(loginModel.TenancyName, loginModel.HostLogin);
			loginModel.UsernameOrEmailAddress = await ResolveLoginNameAsync(loginModel.UsernameOrEmailAddress, loginModel.TenancyName);
			AuthenticationTicket ticket = new AuthenticationTicket((await GetLoginResultAsync(loginModel.UsernameOrEmailAddress, loginModel.Password, loginModel.TenancyName)).Identity, new AuthenticationProperties());
			DateTimeOffset currentUtc = new SystemClock().UtcNow;
			ticket.Properties.IssuedUtc = currentUtc;
			ticket.Properties.ExpiresUtc = currentUtc.Add(TimeSpan.FromMinutes(43200.0));
			return new AjaxResponse(OAuthBearerOptions.AccessTokenFormat.Protect(ticket));
		}

		private async Task<string> ResolveLoginNameAsync(string identifier, string tenancyName)
		{
			if (string.IsNullOrWhiteSpace(identifier) || !identifier.Any(char.IsDigit) || identifier.Any((char character) => !char.IsDigit(character) && character != '+' && character != '-' && character != '(' && character != ')' && !char.IsWhiteSpace(character)))
			{
				return identifier;
			}
			int? tenantId = null;
			if (!string.IsNullOrWhiteSpace(tenancyName))
			{
				using (_unitOfWorkManager.Current.SetTenantId(null))
				{
					Tenant tenant = await _tenantManager.FindByTenancyNameAsync(tenancyName.Trim());
					if (tenant == null)
					{
						return identifier;
					}
					tenantId = tenant.Id;
				}
			}
			using (_unitOfWorkManager.Current.SetTenantId(tenantId))
			{
				string normalizedPhone = StockPilot.Authorization.Users.User.NormalizePhoneNumber(identifier);
				return (from user in _userManager.Users
					where user.PhoneNumber == normalizedPhone
					select user.UserName).FirstOrDefault() ?? identifier;
			}
		}

		private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress, string password, string tenancyName)
		{
			AbpLoginResult<Tenant, User> loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName);
			if (loginResult.Result == AbpLoginResultType.Success)
			{
				return loginResult;
			}
			throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, usernameOrEmailAddress, tenancyName);
		}
	}
}
