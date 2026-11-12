using System.Collections.Generic;
using System.Configuration;
using Abp.Configuration;
using Abp.Json;
using StockPilot.Security;

namespace StockPilot.Configuration
{
	public class AppSettingProvider : SettingProvider
	{
		public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
		{
			context.Manager.GetSettingDefinition("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled").DefaultValue = false.ToString().ToLowerInvariant();
			PasswordComplexitySetting defaultPasswordComplexitySetting = new PasswordComplexitySetting
			{
				MinLength = 6,
				MaxLength = 128,
				UseNumbers = true,
				UseUpperCaseLetters = false,
				UseLowerCaseLetters = true,
				UsePunctuations = false
			};
			return new SettingDefinition[16]
			{
				new SettingDefinition("App.TenantManagement.AllowSelfRegistration", ConfigurationManager.AppSettings["App.TenantManagement.AllowSelfRegistration"] ?? "true"),
				new SettingDefinition("App.TenantManagement.IsNewRegisteredTenantActiveByDefault", ConfigurationManager.AppSettings["App.TenantManagement.IsNewRegisteredTenantActiveByDefault"] ?? "false"),
				new SettingDefinition("App.TenantManagement.UseCaptchaOnRegistration", ConfigurationManager.AppSettings["App.TenantManagement.UseCaptchaOnRegistration"] ?? "true"),
				new SettingDefinition("App.TenantManagement.DefaultEdition", ConfigurationManager.AppSettings["App.TenantManagement.DefaultEdition"] ?? ""),
				new SettingDefinition("App.TenantManagement.RequireApprovalForNewTenants", ConfigurationManager.AppSettings["App.TenantManagement.RequireApprovalForNewTenants"] ?? "true", null, null, null, SettingScopes.Application, isVisibleToClients: true),
				new SettingDefinition("App.TenantManagement.HostLoginMode", ConfigurationManager.AppSettings["App.TenantManagement.HostLoginMode"] ?? "SeparatePage", null, null, null, SettingScopes.Application, isVisibleToClients: true),
				new SettingDefinition("App.Security.PasswordComplexity", defaultPasswordComplexitySetting.ToJsonString(), null, null, null, SettingScopes.Application | SettingScopes.Tenant),
				new SettingDefinition("App.UserManagement.AllowSelfRegistration", ConfigurationManager.AppSettings["App.UserManagement.AllowSelfRegistration"] ?? "true", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.UserManagement.IsNewRegisteredUserActiveByDefault", ConfigurationManager.AppSettings["App.UserManagement.IsNewRegisteredUserActiveByDefault"] ?? "false", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.UserManagement.UseCaptchaOnRegistration", ConfigurationManager.AppSettings["App.UserManagement.UseCaptchaOnRegistration"] ?? "true", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.Subscription.GraceDays", ConfigurationManager.AppSettings["App.Subscription.GraceDays"] ?? "7"),
				new SettingDefinition("App.Subscription.WarnBeforeDays", ConfigurationManager.AppSettings["App.Subscription.WarnBeforeDays"] ?? "7"),
				new SettingDefinition("App.Ecomm.WillUseServerImage", ConfigurationManager.AppSettings["App.Ecomm.WillUseServerImage"] ?? "false", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.Ecomm.WillUseCloudinaryImage", ConfigurationManager.AppSettings["App.Ecomm.WillUseCloudinaryImage"] ?? "false", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.Ecomm.WebSiteAddress", ConfigurationManager.AppSettings["App.Ecomm.WebSiteAddress"] ?? "", null, null, null, SettingScopes.Tenant),
				new SettingDefinition("App.Ecomm.ImageServerUrl", ConfigurationManager.AppSettings["App.Ecomm.ImageServerUrl"] ?? "http://img.a2zmanager.com/item", null, null, null, SettingScopes.Tenant)
			};
		}
	}
}
