using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Json;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.Zero.Ldap.Configuration;
using Newtonsoft.Json;
using StockPilot.Configuration.Host.Dto;
using StockPilot.Configuration.Tenants.Dto;
using StockPilot.MultiTenancy;
using StockPilot.Security;
using StockPilot.Storage;
using StockPilot.Timing;

namespace StockPilot.Configuration.Tenants
{
	[AbpAuthorize(new string[] { "Pages.Administration.Tenant.Settings" })]
	public class TenantSettingsAppService : StockPilotAppServiceBase, ITenantSettingsAppService, IApplicationService, ITransientDependency
	{
		private readonly IMultiTenancyConfig _multiTenancyConfig;

		private readonly IAbpZeroLdapModuleConfig _ldapModuleConfig;

		private readonly ITimeZoneService _timeZoneService;

		private readonly IBinaryObjectManager _binaryObjectManager;

		public TenantSettingsAppService(IMultiTenancyConfig multiTenancyConfig, IAbpZeroLdapModuleConfig ldapModuleConfig, ITimeZoneService timeZoneService, IBinaryObjectManager binaryObjectManager)
		{
			_multiTenancyConfig = multiTenancyConfig;
			_ldapModuleConfig = ldapModuleConfig;
			_timeZoneService = timeZoneService;
			_binaryObjectManager = binaryObjectManager;
		}

		public async Task<TenantSettingsEditDto> GetAllSettings()
		{
			TenantSettingsEditDto tenantSettingsEditDto = new TenantSettingsEditDto();
			TenantSettingsEditDto tenantSettingsEditDto2 = tenantSettingsEditDto;
			tenantSettingsEditDto2.UserManagement = await GetUserManagementSettingsAsync();
			TenantSettingsEditDto tenantSettingsEditDto3 = tenantSettingsEditDto;
			tenantSettingsEditDto3.Security = await GetSecuritySettingsAsync();
			TenantSettingsEditDto settings = tenantSettingsEditDto;
			if (!_multiTenancyConfig.IsEnabled || Clock.SupportsMultipleTimezone)
			{
				tenantSettingsEditDto = settings;
				tenantSettingsEditDto.General = await GetGeneralSettingsAsync();
			}
			if (!_multiTenancyConfig.IsEnabled)
			{
				tenantSettingsEditDto = settings;
				tenantSettingsEditDto.Email = await GetEmailSettingsAsync();
				if (_ldapModuleConfig.IsEnabled)
				{
					tenantSettingsEditDto = settings;
					tenantSettingsEditDto.Ldap = await GetLdapSettingsAsync();
				}
				else
				{
					settings.Ldap = new LdapSettingsEditDto
					{
						IsModuleEnabled = false
					};
				}
			}
			tenantSettingsEditDto = settings;
			tenantSettingsEditDto.Ecomm = await GetEcommSettingsAsync();
			return settings;
		}

		private async Task<LdapSettingsEditDto> GetLdapSettingsAsync()
		{
			LdapSettingsEditDto ldapSettingsEditDto = new LdapSettingsEditDto
			{
				IsModuleEnabled = true
			};
			LdapSettingsEditDto ldapSettingsEditDto2 = ldapSettingsEditDto;
			ldapSettingsEditDto2.IsEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.Ldap.IsEnabled");
			LdapSettingsEditDto ldapSettingsEditDto3 = ldapSettingsEditDto;
			ldapSettingsEditDto3.Domain = await base.SettingManager.GetSettingValueAsync("Abp.Zero.Ldap.Domain");
			LdapSettingsEditDto ldapSettingsEditDto4 = ldapSettingsEditDto;
			ldapSettingsEditDto4.UserName = await base.SettingManager.GetSettingValueAsync("Abp.Zero.Ldap.UserName");
			LdapSettingsEditDto ldapSettingsEditDto5 = ldapSettingsEditDto;
			ldapSettingsEditDto5.Password = await base.SettingManager.GetSettingValueAsync("Abp.Zero.Ldap.Password");
			return ldapSettingsEditDto;
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

		private async Task<GeneralSettingsEditDto> GetGeneralSettingsAsync()
		{
			GeneralSettingsEditDto settings = new GeneralSettingsEditDto();
			if (Clock.SupportsMultipleTimezone)
			{
				string timezone = (settings.Timezone = await base.SettingManager.GetSettingValueForTenantAsync("Abp.Timing.TimeZone", base.AbpSession.GetTenantId()));
				settings.TimezoneForComparison = timezone;
			}
			string defaultTimeZoneId = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.Tenant, base.AbpSession.TenantId);
			if (settings.Timezone == defaultTimeZoneId)
			{
				settings.Timezone = string.Empty;
			}
			return settings;
		}

		private async Task<TenantUserManagementSettingsEditDto> GetUserManagementSettingsAsync()
		{
			TenantUserManagementSettingsEditDto tenantUserManagementSettingsEditDto = new TenantUserManagementSettingsEditDto();
			TenantUserManagementSettingsEditDto tenantUserManagementSettingsEditDto2 = tenantUserManagementSettingsEditDto;
			tenantUserManagementSettingsEditDto2.AllowSelfRegistration = await base.SettingManager.GetSettingValueAsync<bool>("App.UserManagement.AllowSelfRegistration");
			TenantUserManagementSettingsEditDto tenantUserManagementSettingsEditDto3 = tenantUserManagementSettingsEditDto;
			tenantUserManagementSettingsEditDto3.IsNewRegisteredUserActiveByDefault = await base.SettingManager.GetSettingValueAsync<bool>("App.UserManagement.IsNewRegisteredUserActiveByDefault");
			TenantUserManagementSettingsEditDto tenantUserManagementSettingsEditDto4 = tenantUserManagementSettingsEditDto;
			tenantUserManagementSettingsEditDto4.IsEmailConfirmationRequiredForLogin = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin");
			TenantUserManagementSettingsEditDto tenantUserManagementSettingsEditDto5 = tenantUserManagementSettingsEditDto;
			tenantUserManagementSettingsEditDto5.UseCaptchaOnRegistration = await base.SettingManager.GetSettingValueAsync<bool>("App.UserManagement.UseCaptchaOnRegistration");
			return tenantUserManagementSettingsEditDto;
		}

		private async Task<SecuritySettingsEditDto> GetSecuritySettingsAsync()
		{
			string passwordComplexitySetting = await base.SettingManager.GetSettingValueAsync("App.Security.PasswordComplexity");
			string defaultPasswordComplexitySetting = await base.SettingManager.GetSettingValueForApplicationAsync("App.Security.PasswordComplexity");
			SecuritySettingsEditDto securitySettingsEditDto = new SecuritySettingsEditDto
			{
				UseDefaultPasswordComplexitySettings = (passwordComplexitySetting == defaultPasswordComplexitySetting),
				PasswordComplexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(passwordComplexitySetting),
				DefaultPasswordComplexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(defaultPasswordComplexitySetting)
			};
			SecuritySettingsEditDto securitySettingsEditDto2 = securitySettingsEditDto;
			securitySettingsEditDto2.UserLockOut = await GetUserLockOutSettingsAsync();
			SecuritySettingsEditDto settings = securitySettingsEditDto;
			securitySettingsEditDto = settings;
			securitySettingsEditDto.TwoFactorLogin = await GetTwoFactorLoginSettingsAsync();
			return settings;
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

		private Task<bool> IsTwoFactorLoginEnabledForApplicationAsync()
		{
			return base.SettingManager.GetSettingValueForApplicationAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled");
		}

		private async Task<TwoFactorLoginSettingsEditDto> GetTwoFactorLoginSettingsAsync()
		{
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto = new TwoFactorLoginSettingsEditDto();
			TwoFactorLoginSettingsEditDto twoFactorLoginSettingsEditDto2 = twoFactorLoginSettingsEditDto;
			twoFactorLoginSettingsEditDto2.IsEnabledForApplication = await IsTwoFactorLoginEnabledForApplicationAsync();
			TwoFactorLoginSettingsEditDto settings = twoFactorLoginSettingsEditDto;
			if (_multiTenancyConfig.IsEnabled && !settings.IsEnabledForApplication)
			{
				return settings;
			}
			twoFactorLoginSettingsEditDto = settings;
			twoFactorLoginSettingsEditDto.IsEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled");
			twoFactorLoginSettingsEditDto = settings;
			twoFactorLoginSettingsEditDto.IsRememberBrowserEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled");
			if (!_multiTenancyConfig.IsEnabled)
			{
				twoFactorLoginSettingsEditDto = settings;
				twoFactorLoginSettingsEditDto.IsEmailProviderEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEmailProviderEnabled");
				twoFactorLoginSettingsEditDto = settings;
				twoFactorLoginSettingsEditDto.IsSmsProviderEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsSmsProviderEnabled");
			}
			return settings;
		}

		private async Task<EcommSettingsEditDto> GetEcommSettingsAsync()
		{
			bool willUseServerImage;
			try
			{
				bool.TryParse(await base.SettingManager.GetSettingValueAsync("App.Ecomm.WillUseServerImage"), out willUseServerImage);
			}
			catch (Exception)
			{
				willUseServerImage = false;
			}
			bool willUseCloudinaryImage;
			try
			{
				bool.TryParse(await base.SettingManager.GetSettingValueAsync("App.Ecomm.WillUseCloudinaryImage"), out willUseCloudinaryImage);
			}
			catch (Exception)
			{
				willUseCloudinaryImage = false;
			}
			string webSiteAddress;
			try
			{
				webSiteAddress = await base.SettingManager.GetSettingValueAsync("App.Ecomm.WebSiteAddress");
			}
			catch (Exception)
			{
				webSiteAddress = string.Empty;
			}
			string imageServerUrl;
			try
			{
				imageServerUrl = await base.SettingManager.GetSettingValueAsync("App.Ecomm.ImageServerUrl");
			}
			catch (Exception)
			{
				imageServerUrl = string.Empty;
			}
			return new EcommSettingsEditDto
			{
				WillUseServerImage = willUseServerImage,
				WillUseCloudinaryImage = willUseCloudinaryImage,
				WebSiteAddress = webSiteAddress,
				ImageServerUrl = imageServerUrl
			};
		}

		public async Task UpdateAllSettings(TenantSettingsEditDto input)
		{
			await UpdateUserManagementSettingsAsync(input.UserManagement);
			await UpdateSecuritySettingsAsync(input.Security);
			if (Clock.SupportsMultipleTimezone)
			{
				if (!input.General.Timezone.IsNullOrEmpty())
				{
					await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Timing.TimeZone", input.General.Timezone);
				}
				else
				{
					string defaultValue = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.Tenant, base.AbpSession.TenantId);
					await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Timing.TimeZone", defaultValue);
				}
			}
			if (!_multiTenancyConfig.IsEnabled)
			{
				input.ValidateHostSettings();
				await UpdateEmailSettingsAsync(input.Email);
				if (_ldapModuleConfig.IsEnabled)
				{
					await UpdateLdapSettingsAsync(input.Ldap);
				}
			}
			if (input.Ecomm != null)
			{
				await UpdateEcommSettingsAsync(input.Ecomm);
			}
		}

		private async Task UpdateLdapSettingsAsync(LdapSettingsEditDto input)
		{
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.Ldap.IsEnabled", input.IsEnabled.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.Ldap.Domain", input.Domain.IsNullOrWhiteSpace() ? null : input.Domain);
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.Ldap.UserName", input.UserName.IsNullOrWhiteSpace() ? null : input.UserName);
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.Ldap.Password", input.Password.IsNullOrWhiteSpace() ? null : input.Password);
		}

		private async Task UpdateEmailSettingsAsync(EmailSettingsEditDto input)
		{
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.DefaultFromAddress", input.DefaultFromAddress);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.DefaultFromDisplayName", input.DefaultFromDisplayName);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Host", input.SmtpHost);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Port", input.SmtpPort.ToString(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.UserName", input.SmtpUserName);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Password", SimpleStringCipher.Instance.Encrypt(input.SmtpPassword));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.Domain", input.SmtpDomain);
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.EnableSsl", input.SmtpEnableSsl.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForApplicationAsync("Abp.Net.Mail.Smtp.UseDefaultCredentials", input.SmtpUseDefaultCredentials.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
		}

		private async Task UpdateUserManagementSettingsAsync(TenantUserManagementSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.UserManagement.AllowSelfRegistration", settings.AllowSelfRegistration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.UserManagement.IsNewRegisteredUserActiveByDefault", settings.IsNewRegisteredUserActiveByDefault.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.IsEmailConfirmationRequiredForLogin", settings.IsEmailConfirmationRequiredForLogin.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.UserManagement.UseCaptchaOnRegistration", settings.UseCaptchaOnRegistration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
		}

		private async Task UpdateSecuritySettingsAsync(SecuritySettingsEditDto settings)
		{
			if (!settings.UseDefaultPasswordComplexitySettings)
			{
				await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Security.PasswordComplexity", settings.PasswordComplexity.ToJsonString());
			}
			else
			{
				await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Security.PasswordComplexity", settings.DefaultPasswordComplexity.ToJsonString());
			}
			await UpdateUserLockOutSettingsAsync(settings.UserLockOut);
			await UpdateTwoFactorLoginSettingsAsync(settings.TwoFactorLogin);
		}

		private async Task UpdateUserLockOutSettingsAsync(UserLockOutSettingsEditDto settings)
		{
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.UserLockOut.IsEnabled", settings.IsEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.UserLockOut.DefaultAccountLockoutSeconds", settings.DefaultAccountLockoutSeconds.ToString());
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.UserLockOut.MaxFailedAccessAttemptsBeforeLockout", settings.MaxFailedAccessAttemptsBeforeLockout.ToString());
		}

		private async Task UpdateTwoFactorLoginSettingsAsync(TwoFactorLoginSettingsEditDto settings)
		{
			bool flag = _multiTenancyConfig.IsEnabled;
			if (flag)
			{
				flag = !(await IsTwoFactorLoginEnabledForApplicationAsync());
			}
			if (!flag)
			{
				await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled", settings.IsEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
				await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled", settings.IsRememberBrowserEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
				if (!_multiTenancyConfig.IsEnabled)
				{
					await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.TwoFactorLogin.IsEmailProviderEnabled", settings.IsEmailProviderEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
					await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "Abp.Zero.UserManagement.TwoFactorLogin.IsSmsProviderEnabled", settings.IsSmsProviderEnabled.ToString(CultureInfo.InvariantCulture).ToLower());
				}
			}
		}

		private async Task UpdateEcommSettingsAsync(EcommSettingsEditDto input)
		{
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Ecomm.WillUseServerImage", input.WillUseServerImage.ToString());
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Ecomm.WillUseCloudinaryImage", input.WillUseCloudinaryImage.ToString());
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Ecomm.WebSiteAddress", input.WebSiteAddress.IsNullOrWhiteSpace() ? string.Empty : input.WebSiteAddress);
			await base.SettingManager.ChangeSettingForTenantAsync(base.AbpSession.GetTenantId(), "App.Ecomm.ImageServerUrl", input.ImageServerUrl.IsNullOrWhiteSpace() ? string.Empty : input.ImageServerUrl.Trim().TrimEnd('/'));
		}

		public async Task ClearLogo()
		{
			Tenant tenant = await GetCurrentTenantAsync();
			if (tenant.HasLogo())
			{
				if (await _binaryObjectManager.GetOrNullAsync(tenant.LogoId.Value) != null)
				{
					await _binaryObjectManager.DeleteAsync(tenant.LogoId.Value);
				}
				tenant.ClearLogo();
			}
		}

		public async Task ClearCustomCss()
		{
			Tenant tenant = await GetCurrentTenantAsync();
			if (tenant.CustomCssId.HasValue)
			{
				if (await _binaryObjectManager.GetOrNullAsync(tenant.CustomCssId.Value) != null)
				{
					await _binaryObjectManager.DeleteAsync(tenant.CustomCssId.Value);
				}
				tenant.CustomCssId = null;
			}
		}
	}
}
