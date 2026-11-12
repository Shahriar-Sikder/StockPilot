using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Notifications;
using Abp.Runtime.Caching;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.Threading;
using Abp.Timing;
using Abp.UI;
using Abp.Web.Models;
using Abp.Web.Mvc.Authorization;
using Abp.Web.Security.AntiForgery;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using StockPilot.Authorization;
using StockPilot.Authorization.Impersonation;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.Debugging;
using StockPilot.MultiTenancy;
using StockPilot.Notifications;
using StockPilot.Security;
using StockPilot.Web.Auth;
using StockPilot.Web.Controllers.Results;
using StockPilot.Web.Models.Account;
using StockPilot.Web.Models.Layout;
using StockPilot.Web.MultiTenancy;

namespace StockPilot.Web.Controllers
{
	public class AccountController : StockPilotControllerBase
	{
		private readonly LogInManager _logInManager;

		private readonly UserManager _userManager;

		private readonly RoleManager _roleManager;

		private readonly TenantManager _tenantManager;

		private readonly IMultiTenancyConfig _multiTenancyConfig;

		private readonly IUserEmailer _userEmailer;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		private readonly ITenancyNameFinder _tenancyNameFinder;

		private readonly ICacheManager _cacheManager;

		private readonly IWebUrlService _webUrlService;

		private readonly IAppNotifier _appNotifier;

		private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;

		private readonly IUserLinkManager _userLinkManager;

		private readonly INotificationSubscriptionManager _notificationSubscriptionManager;

		private readonly SignInManager _signInManager;

		private readonly IAuthenticationManager _authenticationManager;

		private readonly ILanguageManager _languageManager;

		private readonly IUserPolicy _userPolicy;

		public UserAccessPolicy UserAccessPolicy { get; set; }

		public HostLoginPolicy HostLoginPolicy { get; set; }

		public AccountController(LogInManager logInManager, UserManager userManager, IMultiTenancyConfig multiTenancyConfig, IUserEmailer userEmailer, RoleManager roleManager, TenantManager tenantManager, IUnitOfWorkManager unitOfWorkManager, ITenancyNameFinder tenancyNameFinder, ICacheManager cacheManager, IAppNotifier appNotifier, IWebUrlService webUrlService, AbpLoginResultTypeHelper abpLoginResultTypeHelper, IUserLinkManager userLinkManager, INotificationSubscriptionManager notificationSubscriptionManager, SignInManager signInManager, IAuthenticationManager authenticationManager, ILanguageManager languageManager, IUserPolicy userPolicy)
		{
			_userManager = userManager;
			_multiTenancyConfig = multiTenancyConfig;
			_userEmailer = userEmailer;
			_roleManager = roleManager;
			_tenantManager = tenantManager;
			_unitOfWorkManager = unitOfWorkManager;
			_tenancyNameFinder = tenancyNameFinder;
			_cacheManager = cacheManager;
			_webUrlService = webUrlService;
			_appNotifier = appNotifier;
			_abpLoginResultTypeHelper = abpLoginResultTypeHelper;
			_userLinkManager = userLinkManager;
			_notificationSubscriptionManager = notificationSubscriptionManager;
			_signInManager = signInManager;
			_authenticationManager = authenticationManager;
			_languageManager = languageManager;
			_userPolicy = userPolicy;
			_logInManager = logInManager;
		}

		public ActionResult Login(string userNameOrEmailAddress = "", string returnUrl = "", string successMessage = "")
		{
			return Redirect(base.Url.Content("~/react/index.html#/login"));
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<JsonResult> Login(LoginViewModel loginModel)
		{
			string returnUrl = base.Request["returnUrl"] ?? string.Empty;
			returnUrl = NormalizeReturnUrl(returnUrl);
			await HostLoginPolicy.CheckAsync(loginModel.TenancyName, loginModel.HostLogin);
			AbpLoginResult<Tenant, User> loginResult = await GetLoginResultAsync(await ResolveLoginNameAsync(loginModel.UsernameOrEmailAddress, loginModel.TenancyName), loginModel.Password, loginModel.TenancyName);
			int? tenantId = ((loginResult.Tenant == null) ? ((int?)null) : new int?(loginResult.Tenant.Id));
			using (base.UnitOfWorkManager.Current.SetTenantId(tenantId))
			{
				if (loginResult.User.ShouldChangePasswordOnNextLogin)
				{
					loginResult.User.SetNewPasswordResetCode();
					return Json(new AjaxResponse
					{
						TargetUrl = base.Url.Action("ResetPassword", new ResetPasswordViewModel
						{
							TenantId = SimpleStringCipher.Instance.Encrypt((!tenantId.HasValue) ? null : tenantId.ToString()),
							UserId = SimpleStringCipher.Instance.Encrypt(loginResult.User.Id.ToString()),
							ResetCode = loginResult.User.PasswordResetCode
						})
					});
				}
				if (await _signInManager.SignInOrTwoFactor(loginResult, loginModel.RememberMe) == SignInStatus.RequiresVerification)
				{
					return Json(new AjaxResponse
					{
						TargetUrl = base.Url.Action("SendSecurityCode", new
						{
							returnUrl = returnUrl,
							rememberMe = loginModel.RememberMe
						})
					});
				}
				await base.UnitOfWorkManager.Current.SaveChangesAsync();
				return Json(new AjaxResponse
				{
					TargetUrl = returnUrl
				});
			}
		}

		public ActionResult Logout()
		{
			_authenticationManager.SignOutAll();
			return RedirectToAction("Login");
		}

		private async Task SignInAsync(User user, ClaimsIdentity identity = null, bool rememberMe = false)
		{
			if (identity == null)
			{
				identity = await _userManager.CreateIdentityAsync(user, "ApplicationCookie");
			}
			_authenticationManager.SignOutAllAndSignIn(identity, rememberMe);
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

		private async Task<string> ResolveLoginNameAsync(string identifier, string tenancyName)
		{
			if (identifier.IsNullOrWhiteSpace() || !identifier.Any(char.IsDigit) || identifier.Any((char character) => !char.IsDigit(character) && character != '+' && character != '-' && character != '(' && character != ')' && !char.IsWhiteSpace(character)))
			{
				return identifier;
			}
			int? tenantId = base.AbpSession.TenantId;
			if (!tenancyName.IsNullOrWhiteSpace())
			{
				using (base.CurrentUnitOfWork.SetTenantId(null))
				{
					Tenant tenant = await _tenantManager.FindByTenancyNameAsync(tenancyName.Trim());
					if (tenant == null)
					{
						return identifier;
					}
					tenantId = tenant.Id;
				}
			}
			using (base.CurrentUnitOfWork.SetTenantId(tenantId))
			{
				string normalizedPhoneNumber = StockPilot.Authorization.Users.User.NormalizePhoneNumber(identifier);
				return (await (from user in _userManager.Users
					where user.PhoneNumber == normalizedPhoneNumber
					select user.UserName).FirstOrDefaultAsync()) ?? identifier;
			}
		}

		public async Task<ActionResult> SendSecurityCode(string returnUrl, bool rememberMe)
		{
			long userId = await _signInManager.GetVerifiedUserIdAsync();
			if (userId <= 0)
			{
				return RedirectToAction("Login");
			}
			int? tenantId = await _signInManager.GetVerifiedTenantIdAsync();
			using (base.UnitOfWorkManager.Current.SetTenantId(tenantId))
			{
				(await _userManager.GetValidTwoFactorProvidersAsync(userId)).Select((string userProvider) => new SelectListItem
				{
					Text = userProvider,
					Value = userProvider
				}).ToList();
				return Redirect(base.Url.Content("~/react/index.html#/account?twoFactor=1"));
			}
		}

		[HttpPost]
		public async Task<ActionResult> SendSecurityCode(SendSecurityCodeViewModel model)
		{
			int? tenantId = await _signInManager.GetVerifiedTenantIdAsync();
			using (base.CurrentUnitOfWork.SetTenantId(tenantId))
			{
				if (!(await _signInManager.SendTwoFactorCodeAsync(model.SelectedProvider)))
				{
					throw new UserFriendlyException(L("SendSecurityCodeErrorMessage"));
				}
				return RedirectToAction("VerifySecurityCode", new
				{
					provider = model.SelectedProvider,
					returnUrl = model.ReturnUrl,
					rememberMe = model.RememberMe
				});
			}
		}

		public async Task<ActionResult> VerifySecurityCode(string provider, string returnUrl, bool rememberMe)
		{
			if (!(await _signInManager.HasBeenVerifiedAsync()))
			{
				throw new UserFriendlyException(L("VerifySecurityCodeNotLoggedInErrorMessage"));
			}
			int? tenantId = await _signInManager.GetVerifiedTenantIdAsync();
			if (tenantId.HasValue)
			{
				await base.SettingManager.GetSettingValueForTenantAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled", tenantId.Value);
			}
			else
			{
				await base.SettingManager.GetSettingValueForApplicationAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled");
			}
			return Redirect(base.Url.Content("~/react/index.html#/account?twoFactor=1"));
		}

		[HttpPost]
		public async Task<JsonResult> VerifySecurityCode(VerifySecurityCodeViewModel model)
		{
			model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
			int? tenantId = await _signInManager.GetVerifiedTenantIdAsync();
			using (base.CurrentUnitOfWork.SetTenantId(tenantId))
			{
				switch (await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser))
				{
				case SignInStatus.Success:
					return Json(new AjaxResponse
					{
						TargetUrl = model.ReturnUrl
					});
				case SignInStatus.LockedOut:
					throw new UserFriendlyException(L("UserLockedOutMessage"));
				default:
					throw new UserFriendlyException(L("InvalidSecurityCode"));
				}
			}
		}

		public ActionResult Register()
		{
			return Redirect(base.Url.Content("~/react/index.html#/account"));
		}

		public ActionResult RegisterView(RegisterViewModel model)
		{
			return Redirect(base.Url.Content("~/react/index.html#/account"));
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<ActionResult> Register(RegisterViewModel model)
		{
			_ = 15;
			try
			{
				CheckSelfRegistrationIsEnabled();
				if (!model.IsExternalLogin && UseCaptchaOnRegistration())
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
				if (!_multiTenancyConfig.IsEnabled)
				{
					model.TenancyName = "Default";
				}
				else if (model.TenancyName.IsNullOrEmpty())
				{
					throw new UserFriendlyException(L("TenantNameCanNotBeEmpty"));
				}
				base.CurrentUnitOfWork.SetTenantId(null);
				Tenant tenant = await GetActiveTenantAsync(model.TenancyName);
				base.CurrentUnitOfWork.SetTenantId(tenant.Id);
				if (!(await base.SettingManager.GetSettingValueForTenantAsync<bool>("App.UserManagement.AllowSelfRegistration", tenant.Id)))
				{
					throw new UserFriendlyException(L("SelfUserRegistrationIsDisabledMessage_Detail"));
				}
				await _userPolicy.CheckMaxUserCountAsync(tenant.Id);
				bool isNewRegisteredUserActiveByDefault = await base.SettingManager.GetSettingValueForTenantAsync<bool>("App.UserManagement.IsNewRegisteredUserActiveByDefault", tenant.Id);
				bool isEmailConfirmationRequiredForLogin = await base.SettingManager.GetSettingValueForTenantAsync<bool>("Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin", tenant.Id);
				User user = new User
				{
					TenantId = tenant.Id,
					Name = model.Name,
					Surname = model.Surname,
					EmailAddress = model.EmailAddress,
					IsActive = isNewRegisteredUserActiveByDefault
				};
				ExternalLoginInfo externalLoginInfo = null;
				if (model.IsExternalLogin)
				{
					externalLoginInfo = await _authenticationManager.GetExternalLoginInfoAsync();
					if (externalLoginInfo == null)
					{
						throw new ApplicationException("Can not external login!");
					}
					user.Logins = new List<UserLogin>
					{
						new UserLogin
						{
							LoginProvider = externalLoginInfo.Login.LoginProvider,
							ProviderKey = externalLoginInfo.Login.ProviderKey,
							TenantId = tenant.Id
						}
					};
					model.UserName = model.EmailAddress;
					model.Password = StockPilot.Authorization.Users.User.CreateRandomPassword();
					if (string.Equals(externalLoginInfo.Email, model.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
					{
						user.IsEmailConfirmed = true;
					}
				}
				else if (model.UserName.IsNullOrEmpty() || model.Password.IsNullOrEmpty())
				{
					throw new UserFriendlyException(L("FormIsNotValidMessage"));
				}
				user.UserName = model.UserName;
				user.Password = new PasswordHasher().HashPassword(model.Password);
				user.Roles = new List<UserRole>();
				foreach (Role defaultRole in await _roleManager.Roles.Where((Role r) => r.IsDefault).ToListAsync())
				{
					user.Roles.Add(new UserRole(tenant.Id, user.Id, defaultRole.Id));
				}
				CheckErrors(await _userManager.CreateAsync(user));
				await _unitOfWorkManager.Current.SaveChangesAsync();
				if (!user.IsEmailConfirmed)
				{
					user.SetNewEmailConfirmationCode();
					await _userEmailer.SendEmailActivationLinkAsync(user);
				}
				await _notificationSubscriptionManager.SubscribeToAllAvailableNotificationsAsync(user.ToUserIdentifier());
				await _appNotifier.WelcomeToTheApplicationAsync(user);
				await _appNotifier.NewUserRegisteredAsync(user);
				if (user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin))
				{
					AbpLoginResult<Tenant, User> loginResult = ((externalLoginInfo == null) ? (await GetLoginResultAsync(user.UserName, model.Password, tenant.TenancyName)) : (await _logInManager.LoginAsync(externalLoginInfo.Login, tenant.TenancyName)));
					if (loginResult.Result == AbpLoginResultType.Success)
					{
						await SignInAsync(loginResult.User, loginResult.Identity);
						return Redirect(base.Url.Content("~/react/index.html#/home"));
					}
					base.Logger.Warn("New registered user could not be login. This should not be normally. login result: " + loginResult.Result);
				}
				return Redirect(base.Url.Content("~/react/index.html#/account?registered=1"));
			}
			catch (UserFriendlyException ex)
			{
				base.Response.StatusCode = 400;
				return Content(ex.Message);
			}
		}

		private bool UseCaptchaOnRegistration()
		{
			if (DebugHelper.IsDebug)
			{
				return false;
			}
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (tenancyName.IsNullOrEmpty())
			{
				return true;
			}
			Tenant tenant = AsyncHelper.RunSync(() => GetActiveTenantAsync(tenancyName));
			return base.SettingManager.GetSettingValueForTenant<bool>("App.UserManagement.UseCaptchaOnRegistration", tenant.Id);
		}

		private void CheckSelfRegistrationIsEnabled()
		{
			if (!IsSelfRegistrationEnabled())
			{
				throw new UserFriendlyException(L("SelfUserRegistrationIsDisabledMessage_Detail"));
			}
		}

		private bool IsSelfRegistrationEnabled()
		{
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (tenancyName.IsNullOrEmpty())
			{
				return !_webUrlService.SupportsTenancyNameInUrl;
			}
			Tenant tenant = AsyncHelper.RunSync(() => GetActiveTenantAsync(tenancyName));
			return base.SettingManager.GetSettingValueForTenant<bool>("App.UserManagement.AllowSelfRegistration", tenant.Id);
		}

		public ActionResult ForgotPassword()
		{
			return Redirect(base.Url.Content("~/react/index.html#/forgot-password"));
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<JsonResult> SendPasswordResetLink(SendPasswordResetLinkViewModel model)
		{
			IActiveUnitOfWork current = base.UnitOfWorkManager.Current;
			current.SetTenantId(await GetTenantIdOrDefault(model.TenancyName));
			User user = await GetUserByChecking(model.EmailAddress);
			user.SetNewPasswordResetCode();
			await _userEmailer.SendPasswordResetLinkAsync(user);
			await base.UnitOfWorkManager.Current.SaveChangesAsync();
			return Json(new AjaxResponse());
		}

		[UnitOfWork]
		public virtual async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			int? tenantId = (model.TenantId.IsNullOrEmpty() ? ((int?)null) : new int?(SimpleStringCipher.Instance.Decrypt(model.TenantId).To<int>()));
			long userId = SimpleStringCipher.Instance.Decrypt(model.UserId).To<long>();
			_unitOfWorkManager.Current.SetTenantId(tenantId);
			User user = await _userManager.GetUserByIdAsync(userId);
			if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != model.ResetCode)
			{
				throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
			}
			model.PasswordComplexitySetting = JsonConvert.DeserializeObject<PasswordComplexitySetting>(await base.SettingManager.GetSettingValueForUserAsync("App.Security.PasswordComplexity", tenantId, userId));
			string query = "?tenantId=" + Uri.EscapeDataString(model.TenantId ?? "") + "&userId=" + Uri.EscapeDataString(model.UserId ?? "") + "&resetCode=" + Uri.EscapeDataString(model.ResetCode ?? "");
			return Redirect(base.Url.Content("~/react/index.html#/reset-password") + query);
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<ActionResult> ResetPassword(ResetPasswordFormViewModel model)
		{
			int? tenantId = (model.TenantId.IsNullOrEmpty() ? ((int?)null) : new int?(SimpleStringCipher.Instance.Decrypt(model.TenantId).To<int>()));
			long userId = Convert.ToInt64(SimpleStringCipher.Instance.Decrypt(model.UserId));
			_unitOfWorkManager.Current.SetTenantId(tenantId);
			User user = await _userManager.GetUserByIdAsync(userId);
			if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != model.ResetCode)
			{
				throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
			}
			user.Password = new PasswordHasher().HashPassword(model.Password);
			user.PasswordResetCode = null;
			user.IsEmailConfirmed = true;
			user.ShouldChangePasswordOnNextLogin = false;
			await _userManager.UpdateAsync(user);
			if (user.IsActive)
			{
				await SignInAsync(user);
			}
			return Redirect(base.Url.Content("~/react/index.html#/home"));
		}

		public ActionResult EmailActivation()
		{
			return Redirect(base.Url.Content("~/react/index.html#/account"));
		}

		[HttpPost]
		[UnitOfWork]
		public virtual async Task<JsonResult> SendEmailActivationLink(SendEmailActivationLinkViewModel model)
		{
			int? tenantId = await GetTenantIdOrDefault(model.TenancyName);
			base.UnitOfWorkManager.Current.SetTenantId(tenantId);
			User user = await GetUserByChecking(model.EmailAddress);
			user.SetNewEmailConfirmationCode();
			await _userEmailer.SendEmailActivationLinkAsync(user);
			return Json(new AjaxResponse());
		}

		[UnitOfWork]
		public virtual async Task<ActionResult> EmailConfirmation(EmailConfirmationViewModel model)
		{
			int? tenantId = (model.TenantId.IsNullOrEmpty() ? ((int?)null) : new int?(SimpleStringCipher.Instance.Decrypt(model.TenantId).To<int>()));
			long userId = Convert.ToInt64(SimpleStringCipher.Instance.Decrypt(model.UserId));
			_unitOfWorkManager.Current.SetTenantId(tenantId);
			User user = await _userManager.GetUserByIdAsync(userId);
			if (user == null || user.EmailConfirmationCode.IsNullOrEmpty() || user.EmailConfirmationCode != model.ConfirmationCode)
			{
				throw new UserFriendlyException(L("InvalidEmailConfirmationCode"), L("InvalidEmailConfirmationCode_Detail"));
			}
			user.IsEmailConfirmed = true;
			user.EmailConfirmationCode = null;
			await _userManager.UpdateAsync(user);
			string text = ((!user.TenantId.HasValue) ? "" : (await _tenantManager.GetByIdAsync(user.TenantId.Value)).TenancyName);
			string tenancyName = text;
			return RedirectToAction("Login", new
			{
				successMessage = L("YourEmailIsConfirmedMessage"),
				tenancyName = tenancyName,
				userNameOrEmailAddress = user.UserName
			});
		}

		[ChildActionOnly]
		public PartialViewResult Languages()
		{
			return PartialView("~/Views/Account/_Languages.cshtml", new LanguagesViewModel
			{
				AllLanguages = _languageManager.GetLanguages(),
				CurrentLanguage = _languageManager.CurrentLanguage
			});
		}

		[ChildActionOnly]
		public PartialViewResult TenantCustomCss()
		{
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (string.IsNullOrEmpty(tenancyName))
			{
				return PartialView("_TenantCustomCss", new TenantCustomCssViewModel
				{
					CustomCssId = null
				});
			}
			using (_unitOfWorkManager.Begin())
			{
				using (_unitOfWorkManager.Current.SetTenantId(null))
				{
					Tenant tenant = _tenantManager.FindByTenancyName(tenancyName);
					return PartialView("_TenantCustomCss", new TenantCustomCssViewModel
					{
						CustomCssId = tenant?.CustomCssId
					});
				}
			}
		}

		[ChildActionOnly]
		public PartialViewResult AppLogo(string appPath, int width = 168, int height = 33)
		{
			base.ViewBag.TenantCustomLogoWidth = width;
			base.ViewBag.TenantCustomLogoHeight = height;
			string tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
			if (string.IsNullOrEmpty(tenancyName))
			{
				base.ViewBag.TenantCustomLogoUrl = appPath + "Common/Images/app-logo-on-dark.png";
			}
			else
			{
				using (_unitOfWorkManager.Begin())
				{
					using (_unitOfWorkManager.Current.SetTenantId(null))
					{
						Tenant tenant = _tenantManager.FindByTenancyName(tenancyName);
						base.ViewBag.TenantCustomLogoUrl = ((tenant != null && tenant.LogoId.HasValue) ? (appPath + "TenantCustomization/GetLogo") : (appPath + "Common/Images/app-logo-on-dark.png"));
					}
				}
			}
			return PartialView("~/Views/Account/_AppLogo.cshtml");
		}

		private async Task<User> GetUserByChecking(string emailAddress)
		{
			User obj = await _userManager.Users.Where((User u) => u.EmailAddress == emailAddress).FirstOrDefaultAsync();
			if (obj == null)
			{
				throw new UserFriendlyException(L("InvalidEmailAddress"));
			}
			return obj;
		}

		private async Task<int?> GetTenantIdOrDefault(string tenancyName)
		{
			return (!tenancyName.IsNullOrEmpty()) ? new int?((await GetActiveTenantAsync(tenancyName)).Id) : base.AbpSession.TenantId;
		}

		private async Task<Tenant> GetActiveTenantAsync(string tenancyName)
		{
			Tenant tenant = await _tenantManager.FindByTenancyNameAsync(tenancyName);
			if (tenant == null)
			{
				throw new UserFriendlyException(L("ThereIsNoTenantDefinedWithName{0}", tenancyName));
			}
			if (!tenant.IsActive)
			{
				throw new UserFriendlyException(L("TenantIsNotActive", tenancyName));
			}
			return tenant;
		}

		[HttpPost]
		public ActionResult ExternalLogin(string provider, string returnUrl)
		{
			return new ChallengeResult(provider, base.Url.Action("ExternalLoginCallback", "Account", new
			{
				ReturnUrl = returnUrl,
				tenancyName = (_tenancyNameFinder.GetCurrentTenancyNameOrNull() ?? "")
			}));
		}

		[UnitOfWork]
		[DisableAbpAntiForgeryTokenValidation]
		public virtual async Task<ActionResult> ExternalLoginCallback(string returnUrl, string tenancyName = "")
		{
			returnUrl = NormalizeReturnUrl(returnUrl);
			ExternalLoginInfo loginInfo = await _authenticationManager.GetExternalLoginInfoAsync();
			if (loginInfo == null)
			{
				return RedirectToAction("Login");
			}
			if (tenancyName.IsNullOrEmpty())
			{
				tenancyName = _tenancyNameFinder.GetCurrentTenancyNameOrNull();
				if (tenancyName.IsNullOrEmpty())
				{
					List<Tenant> tenants = await FindPossibleTenantsOfUserAsync(loginInfo.Login);
					switch (tenants.Count)
					{
					case 0:
						return await RegisterView(loginInfo);
					case 1:
						break;
					default:
						return Redirect(base.Url.Content("~/react/index.html#/login"));
					}
					tenancyName = tenants[0].TenancyName;
				}
			}
			AbpLoginResult<Tenant, User> loginResult = await _logInManager.LoginAsync(loginInfo.Login, tenancyName);
			switch (loginResult.Result)
			{
			case AbpLoginResultType.Success:
				await SignInAsync(loginResult.User, loginResult.Identity, rememberMe: true);
				return Redirect(returnUrl);
			case AbpLoginResultType.UnknownExternalLogin:
				return await RegisterView(loginInfo, tenancyName);
			default:
				throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, loginInfo.Email ?? loginInfo.DefaultUserName, tenancyName);
			}
		}

		private async Task<ActionResult> RegisterView(ExternalLoginInfo loginInfo, string tenancyName = null)
		{
			string name = loginInfo.DefaultUserName;
			string surname = loginInfo.DefaultUserName;
			bool extractedNameAndSurname = TryExtractNameAndSurnameFromClaims(loginInfo.ExternalIdentity.Claims.ToList(), ref name, ref surname);
			RegisterViewModel viewModel = new RegisterViewModel
			{
				TenancyName = tenancyName,
				EmailAddress = loginInfo.Email,
				Name = name,
				Surname = surname,
				IsExternalLogin = true
			};
			if (!tenancyName.IsNullOrEmpty() && extractedNameAndSurname)
			{
				return await Register(viewModel);
			}
			return RegisterView(viewModel);
		}

		[UnitOfWork]
		protected virtual async Task<List<Tenant>> FindPossibleTenantsOfUserAsync(UserLoginInfo login)
		{
			List<User> allUsers;
			using (_unitOfWorkManager.Current.DisableFilter("MayHaveTenant"))
			{
				allUsers = await _userManager.FindAllAsync(login);
			}
			return (from u in allUsers
				where u.TenantId.HasValue
				select AsyncHelper.RunSync(() => _tenantManager.FindByIdAsync(u.TenantId.Value)) into t
				where t != null
				select t).ToList();
		}

		private static bool TryExtractNameAndSurnameFromClaims(List<Claim> claims, ref string name, ref string surname)
		{
			string foundName = null;
			string foundSurname = null;
			Claim givennameClaim = claims.FirstOrDefault((Claim c) => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
			if (givennameClaim != null && !givennameClaim.Value.IsNullOrEmpty())
			{
				foundName = givennameClaim.Value;
			}
			Claim surnameClaim = claims.FirstOrDefault((Claim c) => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");
			if (surnameClaim != null && !surnameClaim.Value.IsNullOrEmpty())
			{
				foundSurname = surnameClaim.Value;
			}
			if (foundName == null || foundSurname == null)
			{
				Claim nameClaim = claims.FirstOrDefault((Claim c) => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
				if (nameClaim != null)
				{
					string nameSurName = nameClaim.Value;
					if (!nameSurName.IsNullOrEmpty())
					{
						int lastSpaceIndex = nameSurName.LastIndexOf(' ');
						if (lastSpaceIndex < 1 || lastSpaceIndex > nameSurName.Length - 2)
						{
							foundName = (foundSurname = nameSurName);
						}
						else
						{
							foundName = nameSurName.Substring(0, lastSpaceIndex);
							foundSurname = nameSurName.Substring(lastSpaceIndex);
						}
					}
				}
			}
			if (!foundName.IsNullOrEmpty())
			{
				name = foundName;
			}
			if (!foundSurname.IsNullOrEmpty())
			{
				surname = foundSurname;
			}
			if (foundName != null)
			{
				return foundSurname != null;
			}
			return false;
		}

		[AbpMvcAuthorize(new string[] { "Pages.Administration.Users.Impersonation", "Pages.Tenants.Impersonation" })]
		[DisableAbpAntiForgeryTokenValidation]
		public virtual async Task<JsonResult> Impersonate(ImpersonateModel model)
		{
			if (base.AbpSession.ImpersonatorUserId.HasValue)
			{
				throw new UserFriendlyException(L("CascadeImpersonationErrorMessage"));
			}
			string requiredPermission = ((!base.AbpSession.TenantId.HasValue && model.TenantId.HasValue) ? "Pages.Tenants.Impersonation" : "Pages.Administration.Users.Impersonation");
			if (!(await IsGrantedAsync(requiredPermission)))
			{
				throw new AbpAuthorizationException(L("ImpersonationNotAllowed"));
			}
			if (base.AbpSession.TenantId.HasValue)
			{
				if (!model.TenantId.HasValue)
				{
					throw new UserFriendlyException(L("FromTenantToHostImpersonationErrorMessage"));
				}
				if (model.TenantId.Value != base.AbpSession.TenantId.Value)
				{
					throw new UserFriendlyException(L("DifferentTenantImpersonationErrorMessage"));
				}
			}
			if (model.TenantId == base.AbpSession.TenantId)
			{
				await UserAccessPolicy.CheckCanAccessUserAsync(model.UserId);
			}
			JsonResult result = await SaveImpersonationTokenAndGetTargetUrl(model.TenantId, model.UserId, isBackToImpersonator: false);
			_authenticationManager.SignOutAll();
			return result;
		}

		[UnitOfWork]
		public virtual async Task<ActionResult> ImpersonateSignIn(string tokenId)
		{
			ImpersonationCacheItem cacheItem = await _cacheManager.GetImpersonationCache().GetOrDefaultAsync(tokenId);
			if (cacheItem == null)
			{
				throw new UserFriendlyException(L("ImpersonationTokenErrorMessage"));
			}
			_unitOfWorkManager.Current.SetTenantId(cacheItem.TargetTenantId);
			User user = await _userManager.FindByIdAsync(cacheItem.TargetUserId);
			ClaimsIdentity identity = await _userManager.CreateIdentityAsync(user, "ApplicationCookie");
			if (!cacheItem.IsBackToImpersonator)
			{
				if (cacheItem.ImpersonatorTenantId.HasValue)
				{
					identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorTenantId, cacheItem.ImpersonatorTenantId.Value.ToString(CultureInfo.InvariantCulture)));
				}
				identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorUserId, cacheItem.ImpersonatorUserId.ToString(CultureInfo.InvariantCulture)));
			}
			_authenticationManager.SignOutAllAndSignIn(identity);
			await _cacheManager.GetImpersonationCache().RemoveAsync(tokenId);
			return Redirect(base.Url.Content("~/react/index.html#/home"));
		}

		public virtual JsonResult IsImpersonatedLogin()
		{
			return Json(new AjaxResponse
			{
				Result = base.AbpSession.ImpersonatorUserId.HasValue
			});
		}

		public virtual async Task<JsonResult> BackToImpersonator()
		{
			if (!base.AbpSession.ImpersonatorUserId.HasValue)
			{
				throw new UserFriendlyException(L("NotImpersonatedLoginErrorMessage"));
			}
			JsonResult result = await SaveImpersonationTokenAndGetTargetUrl(base.AbpSession.ImpersonatorTenantId, base.AbpSession.ImpersonatorUserId.Value, isBackToImpersonator: true);
			_authenticationManager.SignOutAll();
			return result;
		}

		private async Task<JsonResult> SaveImpersonationTokenAndGetTargetUrl(int? tenantId, long userId, bool isBackToImpersonator)
		{
			ImpersonationCacheItem cacheItem = new ImpersonationCacheItem(tenantId, userId, isBackToImpersonator);
			if (!isBackToImpersonator)
			{
				cacheItem.ImpersonatorTenantId = base.AbpSession.TenantId;
				cacheItem.ImpersonatorUserId = base.AbpSession.GetUserId();
			}
			string tokenId = Guid.NewGuid().ToString();
			await _cacheManager.GetImpersonationCache().SetAsync(tokenId, cacheItem, TimeSpan.FromMinutes(1.0));
			string tenancyName = null;
			if (tenantId.HasValue)
			{
				tenancyName = (await _tenantManager.GetByIdAsync(tenantId.Value)).TenancyName;
			}
			string targetUrl = _webUrlService.GetSiteRootAddress(tenancyName) + "Account/ImpersonateSignIn?tokenId=" + tokenId;
			return Json(new AjaxResponse
			{
				TargetUrl = targetUrl
			});
		}

		[UnitOfWork]
		[AbpMvcAuthorize(new string[] { })]
		public virtual async Task<JsonResult> SwitchToLinkedAccount(SwitchToLinkedAccountModel model)
		{
			if (!(await _userLinkManager.AreUsersLinked(base.AbpSession.ToUserIdentifier(), model.ToUserIdentifier())))
			{
				throw new ApplicationException(L("This account is not linked to your account"));
			}
			JsonResult result = await SaveAccountSwitchTokenAndGetTargetUrl(model.TargetTenantId, model.TargetUserId);
			_authenticationManager.SignOutAll();
			return result;
		}

		[UnitOfWork]
		public virtual async Task<ActionResult> SwitchToLinkedAccountSignIn(string tokenId)
		{
			SwitchToLinkedAccountCacheItem cacheItem = await _cacheManager.GetSwitchToLinkedAccountCache().GetOrDefaultAsync(tokenId);
			if (cacheItem == null)
			{
				throw new UserFriendlyException(L("SwitchToLinkedAccountTokenErrorMessage"));
			}
			_unitOfWorkManager.Current.SetTenantId(cacheItem.TargetTenantId);
			User user = await _userManager.FindByIdAsync(cacheItem.TargetUserId);
			ClaimsIdentity identity = await _userManager.CreateIdentityAsync(user, "ApplicationCookie");
			if (cacheItem.ImpersonatorTenantId.HasValue)
			{
				identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorTenantId, cacheItem.ImpersonatorTenantId.Value.ToString(CultureInfo.InvariantCulture)));
			}
			if (cacheItem.ImpersonatorUserId.HasValue)
			{
				identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorUserId, cacheItem.ImpersonatorUserId.Value.ToString(CultureInfo.InvariantCulture)));
			}
			_authenticationManager.SignOutAllAndSignIn(identity);
			user.LastLoginTime = Clock.Now;
			await _cacheManager.GetSwitchToLinkedAccountCache().RemoveAsync(tokenId);
			return Redirect(base.Url.Content("~/react/index.html#/home"));
		}

		private async Task<JsonResult> SaveAccountSwitchTokenAndGetTargetUrl(int? targetTenantId, long targetUserId)
		{
			SwitchToLinkedAccountCacheItem cacheItem = new SwitchToLinkedAccountCacheItem(targetTenantId, targetUserId, base.AbpSession.ImpersonatorTenantId, base.AbpSession.ImpersonatorUserId);
			string tokenId = Guid.NewGuid().ToString();
			await _cacheManager.GetSwitchToLinkedAccountCache().SetAsync(tokenId, cacheItem, TimeSpan.FromMinutes(1.0));
			string tenancyName = null;
			if (targetTenantId.HasValue)
			{
				tenancyName = (await _tenantManager.GetByIdAsync(targetTenantId.Value)).TenancyName;
			}
			string targetUrl = _webUrlService.GetSiteRootAddress(tenancyName) + "Account/SwitchToLinkedAccountSignIn?tokenId=" + tokenId;
			return Json(new AjaxResponse
			{
				TargetUrl = targetUrl
			});
		}

		private string NormalizeReturnUrl(string returnUrl, Func<string> defaultValueBuilder = null)
		{
			if (defaultValueBuilder == null)
			{
				defaultValueBuilder = () => base.Url.Content("~/react/index.html#/home");
			}
			if (returnUrl.IsNullOrEmpty())
			{
				return defaultValueBuilder();
			}
			if (base.Url.IsLocalUrl(returnUrl))
			{
				return returnUrl;
			}
			return defaultValueBuilder();
		}

		[AbpMvcAuthorize(new string[] { })]
		public async Task<ActionResult> TestNotification(string message = "", string severity = "info")
		{
			if (message.IsNullOrEmpty())
			{
				message = "This is a test notification, created at " + Clock.Now.ToString();
			}
			await _appNotifier.SendMessageAsync(base.AbpSession.ToUserIdentifier(), message, severity.ToPascalCase(CultureInfo.InvariantCulture).ToEnum<NotificationSeverity>());
			return Content("Sent notification: " + message);
		}
	}
}
