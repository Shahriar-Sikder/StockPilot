using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Json;
using Abp.Net.Mail;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.UI;
using Newtonsoft.Json;
using StockPilot.Configuration.Host.Dto;
using StockPilot.Editions;
using StockPilot.Security;
using StockPilot.Timing;

namespace StockPilot.Configuration.Host
{
	[AbpAuthorize(new string[] { "Pages.Administration.Host.Settings" })]
	public class HostSettingsAppService : StockPilotAppServiceBase, IHostSettingsAppService, IApplicationService, ITransientDependency
	{
		private readonly IEmailSender _emailSender;

		private readonly EditionManager _editionManager;

		private readonly ITimeZoneService _timeZoneService;

		private readonly ISettingDefinitionManager _settingDefinitionManager;

		public HostSettingsAppService(IEmailSender emailSender, EditionManager editionManager, ITimeZoneService timeZoneService, ISettingDefinitionManager settingDefinitionManager)
		{
			_emailSender = emailSender;
			_editionManager = editionManager;
			_timeZoneService = timeZoneService;
			_settingDefinitionManager = settingDefinitionManager;
		}

		public async Task<HostSettingsEditDto> GetAllSettings()
		{
			HostSettingsEditDto hostSettingsEditDto = new HostSettingsEditDto();
			HostSettingsEditDto hostSettingsEditDto2 = hostSettingsEditDto;
			hostSettingsEditDto2.General = await GetGeneralSettingsAsync();
			HostSettingsEditDto hostSettingsEditDto3 = hostSettingsEditDto;
			hostSettingsEditDto3.TenantManagement = await GetTenantManagementSettingsAsync();
			HostSettingsEditDto hostSettingsEditDto4 = hostSettingsEditDto;
			hostSettingsEditDto4.UserManagement = await GetUserManagementAsync();
			HostSettingsEditDto hostSettingsEditDto5 = hostSettingsEditDto;
			hostSettingsEditDto5.Email = await GetEmailSettingsAsync();
			HostSettingsEditDto hostSettingsEditDto6 = hostSettingsEditDto;
			hostSettingsEditDto6.Security = await GetSecuritySettingsAsync();
			return hostSettingsEditDto;
		}

		private async Task<GeneralSettingsEditDto> GetGeneralSettingsAsync()
		{
			string timezone = await base.SettingManager.GetSettingValueForApplicationAsync("Abp.Timing.TimeZone");
			GeneralSettingsEditDto settings = new GeneralSettingsEditDto
			{
				Timezone = timezone,
				TimezoneForComparison = timezone
			};
			string defaultTimeZoneId = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.Application, base.AbpSession.TenantId);
			if (settings.Timezone == defaultTimeZoneId)
			{
				settings.Timezone = string.Empty;
			}
			return settings;
		}

		private async Task<TenantManagementSettingsEditDto> GetTenantManagementSettingsAsync()
		{
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto = new TenantManagementSettingsEditDto();
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto2 = tenantManagementSettingsEditDto;
			tenantManagementSettingsEditDto2.AllowSelfRegistration = await base.SettingManager.GetSettingValueAsync<bool>("App.TenantManagement.AllowSelfRegistration");
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto3 = tenantManagementSettingsEditDto;
			tenantManagementSettingsEditDto3.IsNewRegisteredTenantActiveByDefault = await base.SettingManager.GetSettingValueAsync<bool>("App.TenantManagement.IsNewRegisteredTenantActiveByDefault");
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto4 = tenantManagementSettingsEditDto;
			tenantManagementSettingsEditDto4.UseCaptchaOnRegistration = await base.SettingManager.GetSettingValueAsync<bool>("App.TenantManagement.UseCaptchaOnRegistration");
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto5 = tenantManagementSettingsEditDto;
			tenantManagementSettingsEditDto5.RequireApprovalForNewTenants = await base.SettingManager.GetSettingValueAsync<bool>("App.TenantManagement.RequireApprovalForNewTenants");
			TenantManagementSettingsEditDto tenantManagementSettingsEditDto6 = tenantManagementSettingsEditDto;
			tenantManagementSettingsEditDto6.HostLoginMode = await base.SettingManager.GetSettingValueAsync("App.TenantManagement.HostLoginMode");
			TenantManagementSettingsEditDto settings = tenantManagementSettingsEditDto;
			string defaultEditionId = await base.SettingManager.GetSettingValueAsync("App.TenantManagement.DefaultEdition");
			bool flag = !string.IsNullOrEmpty(defaultEditionId);
			if (flag)
			{
				flag = await _editionManager.FindByIdAsync(Convert.ToInt32(defaultEditionId)) != null;
			}
			if (flag)
			{
				settings.DefaultEditionId = Convert.ToInt32(defaultEditionId);
			}
			return settings;
		}

		private async Task<HostUserManagementSettingsEditDto> GetUserManagementAsync()
		{
			HostUserManagementSettingsEditDto hostUserManagementSettingsEditDto = new HostUserManagementSettingsEditDto();
			HostUserManagementSettingsEditDto hostUserManagementSettingsEditDto2 = hostUserManagementSettingsEditDto;
			hostUserManagementSettingsEditDto2.IsEmailConfirmationRequiredForLogin = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin");
			return hostUserManagementSettingsEditDto;
		}

		private async Task<EmailSettingsEditDto> GetEmailSettingsAsync()
		{
			string smtpPassword = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.Smtp.Password");
			EmailSettingsEditDto emailSettingsEditDto = new EmailSettingsEditDto();
			EmailSettingsEditDto emailSettingsEditDto2 = emailSettingsEditDto;
			emailSettingsEditDto2.DefaultFromAddress = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.DefaultFromAddress");
			EmailSettingsEditDto emailSettingsEditDto3 = emailSettingsEditDto;
			emailSettingsEditDto3.DefaultFromDisplayName = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.DefaultFromDisplayName");
			EmailSettingsEditDto emailSettingsEditDto4 = emailSettingsEditDto;
			emailSettingsEditDto4.SmtpHost = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.Smtp.Host");
			EmailSettingsEditDto emailSettingsEditDto5 = emailSettingsEditDto;
			emailSettingsEditDto5.SmtpPort = await base.SettingManager.GetSettingValueAsync<int>("Abp.Net.Mail.Smtp.Port");
			EmailSettingsEditDto emailSettingsEditDto6 = emailSettingsEditDto;
			emailSettingsEditDto6.SmtpUserName = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.Smtp.UserName");
			emailSettingsEditDto.SmtpPassword = SimpleStringCipher.Instance.Decrypt(smtpPassword);
			EmailSettingsEditDto emailSettingsEditDto7 = emailSettingsEditDto;
			emailSettingsEditDto7.SmtpDomain = await base.SettingManager.GetSettingValueAsync("Abp.Net.Mail.Smtp.Domain");
			EmailSettingsEditDto emailSettingsEditDto8 = emailSettingsEditDto;
			emailSettingsEditDto8.SmtpEnableSsl = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Net.Mail.Smtp.EnableSsl");
			EmailSettingsEditDto emailSettingsEditDto9 = emailSettingsEditDto;
			emailSettingsEditDto9.SmtpUseDefaultCredentials = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Net.Mail.Smtp.UseDefaultCredentials");
			return emailSettingsEditDto;
		}

		private async Task<SecuritySettingsEditDto> GetSecuritySettingsAsync()
		{
			string passwordComplexitySetting = await base.SettingManager.GetSettingValueAsync("App.Security.PasswordComplexity");
			string defaultPasswordComplexitySetting = _settingDefinitionManager.GetSettingDefinition("App.Security.PasswordComplexity").DefaultValue;
			SecuritySettingsEditDto securitySettingsEditDto = new SecuritySettingsEditDto
			{
				UseDefaultPasswordComplexitySettings = (passwordComplexitySetting == defaultPasswordComplexitySetting),
				PasswordComplexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(passwordComplexitySetting),
				DefaultPasswordComplexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(defaultPasswordComplexitySetting)
			};
			SecuritySettingsEditDto securitySettingsEditDto2 = securitySettingsEditDto;
			securitySettingsEditDto2.UserLockOut = await GetUserLockOutSettingsAsync();
			SecuritySettingsEditDto securitySettingsEditDto3 = securitySettingsEditDto;
			securitySettingsEditDto3.TwoFactorLogin = await GetTwoFactorLoginSettingsAsync();
			return securitySettingsEditDto;
		}

		private async Task<UserLockOutSettingsEditDto> GetUserLockOutSettingsAsync()
		{
			UserLockOutSettingsEditDto userLockOutSettingsEditDto = new UserLockOutSettingsEditDto();
			UserLockOutSettingsEditDto userLockOutSettingsEditDto2 = userLockOutSettingsEditDto;
			userLockOutSettingsEditDto2.IsEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.UserLockOut.IsEnabled");
			UserLockOutSettingsEditDto userLockOutSettingsEditDto3 = userLockOutSettingsEditDto;
			userLockOutSettingsEditDto3.MaxFailedAccessAttemptsBeforeLockout = await base.SettingManager.GetSettingValueAsync<int>("Abp.Zero.UserManagement.UserLockOut.MaxFailedAccessAttemptsBeforeLockout");
			UserLockOutSettingsEditDto userLockOutSettingsEditDto4 = userLockOutSettingsEditDto;
			userLockOutSettingsEditDto4.DefaultAccountLockoutSeconds = await base.SettingManager.GetSettingValueAsync<int>("Abp.Zero.UserManagement.UserLockOut.DefaultAccountLockoutSeconds");
			return userLockOutSettingsEditDto;
		}

		private async Task<TwoFactorLoginSettingsEditDto> GetTwoFactorLoginSettingsAsync()
		{
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto = new TwoFactorLoginSettingsEditDto();
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto2 = twoFactorLoginSettingsEditDto;
			twoFactorLoginSettingsEditDto2.IsEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled");
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto3 = twoFactorLoginSettingsEditDto;
			twoFactorLoginSettingsEditDto3.IsEmailProviderEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEmailProviderEnabled");
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto4 = twoFactorLoginSettingsEditDto;
			twoFactorLoginSettingsEditDto4.IsSmsProviderEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsSmsProviderEnabled");
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto5 = twoFactorLoginSettingsEditDto;
			twoFactorLoginSettingsEditDto5.IsRememberBrowserEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled");
			return twoFactorLoginSettingsEditDto;
		}

		public async Task UpdateAllSettings(HostSettingsEditDto input)
		{
			await UpdateGeneralSettingsAsync(input.General);
			await UpdateTenantManagementAsync(input.TenantManagement);
			await UpdateUserManagementSettingsAsync(input.UserManagement);
			await UpdateSecuritySettingsAsync(input.Security);
			await UpdateEmailSettingsAsync(input.Email);
		}

		private async Task UpdateGeneralSettingsAsync(GeneralSettingsEditDto settings)
		{
			if (Clock.SupportsMultipleTimezone)
			{
				if (settings.Timezone.IsNullOrEmpty())
				{
					string defaultValue = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.Application, base.AbpSession.TenantId);
					await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Timing.TimeZone", defaultValue);
				}
				else
				{
					await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Timing.TimeZone", settings.Timezone);
				}
			}
		}

		private async Task UpdateTenantManagementAsync(TenantManagementSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.AllowSelfRegistration", settings.AllowSelfRegistration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.IsNewRegisteredTenantActiveByDefault", settings.IsNewRegisteredTenantActiveByDefault.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.UseCaptchaOnRegistration", settings.UseCaptchaOnRegistration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.DefaultEdition", settings.DefaultEditionId?.ToString() ?? "");
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.RequireApprovalForNewTenants", settings.RequireApprovalForNewTenants.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			if (settings.HostLoginMode != "LoginPage" && settings.HostLoginMode != "SeparatePage")
			{
				throw new UserFriendlyException(L("InvalidHostLoginMode"));
			}
			await base.SettingManager.ChangeSettingForApplicationAsync("App.TenantManagement.HostLoginMode", settings.HostLoginMode);
		}

		private async Task UpdateUserManagementSettingsAsync(HostUserManagementSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin", settings.IsEmailConfirmationRequiredForLogin.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
		}

		private async Task UpdateSecuritySettingsAsync(SecuritySettingsEditDto settings)
		{
			if (!settings.UseDefaultPasswordComplexitySettings)
			{
				await base.SettingManager.ChangeSettingForApplicationAsync("App.Security.PasswordComplexity", settings.PasswordComplexity.ToJsonString());
			}
			else
			{
				await base.SettingManager.ChangeSettingForApplicationAsync("App.Security.PasswordComplexity", settings.DefaultPasswordComplexity.ToJsonString());
			}
			await UpdateUserLockOutSettingsAsync(settings.UserLockOut);
			await UpdateTwoFactorLoginSettingsAsync(settings.TwoFactorLogin);
		}

		private async Task UpdateUserLockOutSettingsAsync(UserLockOutSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.UserLockOut.IsEnabled", settings.IsEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.UserLockOut.DefaultAccountLockoutSeconds", settings.DefaultAccountLockoutSeconds.ToString());
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.UserLockOut.MaxFailedAccessAttemptsBeforeLockout", settings.MaxFailedAccessAttemptsBeforeLockout.ToString());
		}

		private async Task UpdateTwoFactorLoginSettingsAsync(TwoFactorLoginSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled", settings.IsEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.TwoFactorLogin.IsEmailProviderEnabled", settings.IsEmailProviderEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.TwoFactorLogin.IsSmsProviderEnabled", settings.IsSmsProviderEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled", settings.IsRememberBrowserEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
		}

		private async Task UpdateEmailSettingsAsync(EmailSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.DefaultFromAddress", settings.DefaultFromAddress);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.DefaultFromDisplayName", settings.DefaultFromDisplayName);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Host", settings.SmtpHost);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Port", settings.SmtpPort.ToString(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.UserName", settings.SmtpUserName);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Password", SimpleStringCipher.Instance.Encrypt(settings.SmtpPassword));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Domain", settings.SmtpDomain);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.EnableSsl", settings.SmtpEnableSsl.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.UseDefaultCredentials", settings.SmtpUseDefaultCredentials.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
		}

		public async Task SendTestEmail(SendTestEmailInput input)
		{
			await _emailSender.SendAsync(input.EmailAddress, L("TestEmail_Subject"), L("TestEmail_Body"));
		}
	}
}
