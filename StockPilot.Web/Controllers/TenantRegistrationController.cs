using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Abp;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using Microsoft.Owin.Security;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using StockPilot.Authorization;
using StockPilot.Authorization.Users;
using StockPilot.Debugging;
using StockPilot.Editions;
using StockPilot.EntityFramework;
using StockPilot.MultiTenancy;
using StockPilot.Notifications;
using StockPilot.Web.Auth;
using StockPilot.Web.Models.TenantRegistration;

namespace StockPilot.Web.Controllers
{
	public class TenantRegistrationController : StockPilotControllerBase
	{
		private readonly IMultiTenancyConfig _multiTenancyConfig;

		private readonly TenantManager _tenantManager;

		private readonly UserManager _userManager;

		private readonly LogInManager _logInManager;

		private readonly EditionManager _editionManager;

		private readonly IAppNotifier _appNotifier;

		private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;

		private readonly StockPilotDbContext _stockPilotDbContext;

		private readonly TenantRequestManager _tenantRequestManager;

		private readonly TenantRequestEmailer _tenantRequestEmailer;

		private readonly IRepository<TenantRequest> _tenantRequestRepository;

		private IAuthenticationManager AuthenticationManager => base.HttpContext.GetOwinContext().Authentication;

		public TenantRegistrationController(IMultiTenancyConfig multiTenancyConfig, TenantManager tenantManager, EditionManager editionManager, IAppNotifier appNotifier, UserManager userManager, AbpLoginResultTypeHelper abpLoginResultTypeHelper, LogInManager logInManager, StockPilotDbContext stockPilotDbContext, TenantRequestManager tenantRequestManager, TenantRequestEmailer tenantRequestEmailer, IRepository<TenantRequest> tenantRequestRepository)
		{
			_tenantRequestManager = tenantRequestManager;
			_tenantRequestEmailer = tenantRequestEmailer;
			_tenantRequestRepository = tenantRequestRepository;
			_multiTenancyConfig = multiTenancyConfig;
			_tenantManager = tenantManager;
			_editionManager = editionManager;
			_appNotifier = appNotifier;
			_userManager = userManager;
			_abpLoginResultTypeHelper = abpLoginResultTypeHelper;
			_logInManager = logInManager;
			_stockPilotDbContext = stockPilotDbContext;
		}

		public ActionResult Index()
		{
			return Redirect(base.Url.Content("~/react/index.html#/register"));
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<ActionResult> Register(TenantRegistrationViewModel model)
		{
			_ = 11;
			try
			{
				CheckTenantRegistrationIsEnabled();
				if (model == null || !base.ModelState.IsValid)
				{
					string validationMessage = string.Join(" ", from error in base.ModelState.Values.SelectMany((ModelState value) => value.Errors)
						select error.ErrorMessage into message
						where !string.IsNullOrWhiteSpace(message)
						select message);
					return RegistrationError(validationMessage.IsNullOrEmpty() ? "Please complete all required fields." : validationMessage, 400);
				}
				if (UseCaptchaOnRegistration())
				{
					RecaptchaVerificationHelper recaptchaVerificationHelper = this.GetRecaptchaVerificationHelper();
					if (recaptchaVerificationHelper.Response.IsNullOrEmpty())
					{
						throw new UserFriendlyException(L("CaptchaCanNotBeEmpty"));
					}
					if (recaptchaVerificationHelper.VerifyRecaptchaResponse() != RecaptchaVerificationResult.Success)
					{
						throw new UserFriendlyException(L("IncorrectCaptchaAnswer"));
					}
				}
				if (await base.SettingManager.GetSettingValueForApplicationAsync<bool>("App.TenantManagement.RequireApprovalForNewTenants"))
				{
					return await SaveRequestAsync(model);
				}
				bool isNewRegisteredTenantActiveByDefault = await base.SettingManager.GetSettingValueForApplicationAsync<bool>("App.TenantManagement.IsNewRegisteredTenantActiveByDefault");
				bool isEmailConfirmationRequiredForLogin = await base.SettingManager.GetSettingValueForApplicationAsync<bool>("Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin");
				string defaultEditionIdValue = await base.SettingManager.GetSettingValueForApplicationAsync("App.TenantManagement.DefaultEdition");
				int? defaultEditionId = null;
				bool flag = !string.IsNullOrEmpty(defaultEditionIdValue);
				if (flag)
				{
					flag = await _editionManager.FindByIdAsync(Convert.ToInt32(defaultEditionIdValue)) != null;
				}
				if (flag)
				{
					defaultEditionId = Convert.ToInt32(defaultEditionIdValue);
				}
				base.CurrentUnitOfWork.SetTenantId(null);
				int tenantId = await _tenantManager.CreateWithAdminUserAsync(model.TenancyName, model.Name, model.AdminPassword, model.AdminEmailAddress, null, isNewRegisteredTenantActiveByDefault, defaultEditionId, shouldChangePasswordOnNextLogin: false, isEmailConfirmationRequiredForLogin, model.AdminPhoneNumber, model.AdminName, model.AdminSurname);
				Tenant tenant = await _tenantManager.GetByIdAsync(tenantId);
				tenant.ContactName = TenantRegistrationViewModel.Clean(model.ContactName);
				tenant.ContactEmail = TenantRegistrationViewModel.Clean(model.ContactEmail);
				tenant.ContactPhone = TenantRegistrationViewModel.Clean(model.ContactPhoneOrAdmin);
				tenant.Address = TenantRegistrationViewModel.Clean(model.Address);
				tenant.City = TenantRegistrationViewModel.Clean(model.City);
				tenant.CountryName = TenantRegistrationViewModel.Clean(model.CountryName);
				tenant.BusinessType = TenantRegistrationViewModel.Clean(model.BusinessType);
				tenant.Notes = TenantRegistrationViewModel.Clean(model.Notes);
				await _appNotifier.NewTenantRegisteredAsync(tenant);
				base.CurrentUnitOfWork.SetTenantId(tenant.Id);
				if (StockPilotDemoSeeder.IsEnabledFor(tenant.Name))
				{
					StockPilotDemoSeeder.Seed(_stockPilotDbContext, tenant.Id);
				}
				User user = await _userManager.FindByNameAsync("admin");
				if (user == null)
				{
					throw new AbpException("The tenant was created, but its administrator account could not be loaded.");
				}
				if (tenant.IsActive && user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin))
				{
					AbpLoginResult<Tenant, User> loginResult = await GetLoginResultAsync(user.UserName, model.AdminPassword, tenant.TenancyName);
					if (loginResult.Result == AbpLoginResultType.Success)
					{
						await SignInAsync(loginResult.User, loginResult.Identity);
						return Json(new
						{
							success = true,
							targetUrl = base.Url.Content("~/react/index.html#/home?created=1")
						});
					}
					base.Logger.Warn("New registered user could not be login. This should not be normally. login result: " + loginResult.Result);
				}
				string targetUrl = (tenant.IsActive ? (base.Url.Content("~/react/index.html#/login?registered=1") + "&tenancyName=" + Uri.EscapeDataString(tenant.TenancyName)) : (base.Url.Content("~/react/index.html#/register?created=1&pending=1") + "&tenancyName=" + Uri.EscapeDataString(tenant.TenancyName)));
				return Json(new
				{
					success = true,
					targetUrl = targetUrl
				});
			}
			catch (AbpException ex)
			{
				return RegistrationError(ex.Message, 400);
			}
			catch (Exception exception)
			{
				base.Logger.Error("Tenant registration failed.", exception);
				return RegistrationError("We could not create the workspace. Please try again later.", 500);
			}
		}

		private async Task<ActionResult> SaveRequestAsync(TenantRegistrationViewModel model)
		{
			base.CurrentUnitOfWork.SetTenantId(null);
			string ipAddress = base.Request.UserHostAddress;
			TenantRequest request = await _tenantRequestManager.CreateAsync(new TenantRequest
			{
				TenancyName = model.TenancyName,
				StoreName = model.Name.Trim(),
				ContactName = TenantRegistrationViewModel.Clean(model.ContactName),
				ContactEmail = TenantRegistrationViewModel.Clean(model.ContactEmail),
				ContactPhone = TenantRegistrationViewModel.Clean(model.ContactPhoneOrAdmin),
				Address = TenantRegistrationViewModel.Clean(model.Address),
				City = TenantRegistrationViewModel.Clean(model.City),
				CountryName = TenantRegistrationViewModel.Clean(model.CountryName),
				BusinessType = TenantRegistrationViewModel.Clean(model.BusinessType),
				Notes = TenantRegistrationViewModel.Clean(model.Notes),
				AdminName = model.AdminName.Trim(),
				AdminSurname = model.AdminSurname.Trim(),
				AdminEmailAddress = model.AdminEmailAddress.Trim(),
				AdminPhoneNumber = StockPilot.Authorization.Users.User.NormalizePhoneNumber(model.AdminPhoneNumber),
				RequesterIpAddress = ((ipAddress != null && ipAddress.Length > 64) ? ipAddress.Substring(0, 64) : ipAddress)
			}, model.AdminPassword);
			await _appNotifier.NewTenantRequestAsync(request);
			await _tenantRequestEmailer.SendReceivedAsync(request);
			return Json(new
			{
				success = true,
				requestNo = request.RequestNo,
				targetUrl = base.Url.Content("~/react/index.html#/register?requested=1&requestNo=") + Uri.EscapeDataString(request.RequestNo)
			});
		}

		[AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
		[UnitOfWork]
		public virtual async Task<ActionResult> RequestStatus(string requestNo, string email)
		{
			if (string.IsNullOrWhiteSpace(requestNo) || string.IsNullOrWhiteSpace(email))
			{
				return RequestNotFound();
			}
			base.CurrentUnitOfWork.SetTenantId(null);
			string number = requestNo.Trim().ToUpperInvariant();
			string address = email.Trim().ToUpperInvariant();
			TenantRequest request = await _tenantRequestRepository.FirstOrDefaultAsync((TenantRequest r) => r.RequestNo.ToUpper() == number && (r.AdminEmailAddress.ToUpper() == address || r.ContactEmail.ToUpper() == address));
			if (request == null)
			{
				return RequestNotFound();
			}
			return Json(new
			{
				success = true,
				result = new
				{
					requestNo = request.RequestNo,
					status = (int)request.Status,
					statusName = request.Status.ToString(),
					reviewNote = ((request.Status == TenantRequestStatus.Rejected) ? request.ReviewNote : null),
					tenancyName = ((request.Status == TenantRequestStatus.Approved) ? request.TenancyName : null)
				}
			}, JsonRequestBehavior.AllowGet);
		}

		private JsonResult RequestNotFound()
		{
			base.Response.StatusCode = 404;
			return Json(new
			{
				success = false,
				error = new
				{
					message = L("TenantRequestNotFound")
				}
			}, JsonRequestBehavior.AllowGet);
		}

		private JsonResult RegistrationError(string message, int statusCode)
		{
			base.Response.StatusCode = statusCode;
			return Json(new
			{
				success = false,
				error = new { message }
			});
		}

		private bool IsSelfRegistrationEnabled()
		{
			return base.SettingManager.GetSettingValueForApplication<bool>("App.TenantManagement.AllowSelfRegistration");
		}

		private void CheckTenantRegistrationIsEnabled()
		{
			if (!IsSelfRegistrationEnabled())
			{
				throw new UserFriendlyException(L("SelfTenantRegistrationIsDisabledMessage_Detail"));
			}
			if (!_multiTenancyConfig.IsEnabled)
			{
				throw new UserFriendlyException(L("MultiTenancyIsNotEnabled"));
			}
		}

		[HttpGet]
		public JsonResult RegistrationOptions()
		{
			string siteKey = ConfigurationManager.AppSettings["recaptchaPublicKey"];
			return Json(new
			{
				success = true,
				result = new
				{
					useCaptcha = UseCaptchaOnRegistration(),
					captchaSiteKey = (string.IsNullOrWhiteSpace(siteKey) ? null : siteKey),
					requireApproval = base.SettingManager.GetSettingValueForApplication<bool>("App.TenantManagement.RequireApprovalForNewTenants")
				}
			}, JsonRequestBehavior.AllowGet);
		}

		private bool UseCaptchaOnRegistration()
		{
			if (DebugHelper.IsDebug && !string.Equals(ConfigurationManager.AppSettings["App.Captcha.UseInDebug"], "true", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return base.SettingManager.GetSettingValueForApplication<bool>("App.TenantManagement.UseCaptchaOnRegistration");
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

		private async Task SignInAsync(User user, ClaimsIdentity identity = null, bool rememberMe = false)
		{
			if (identity == null)
			{
				identity = await _userManager.CreateIdentityAsync(user, "ApplicationCookie");
			}
			AuthenticationManager.SignOutAllAndSignIn(identity, rememberMe);
		}
	}
}
