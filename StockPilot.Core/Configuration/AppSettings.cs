namespace StockPilot.Configuration
{
	public static class AppSettings
	{
		public static class General
		{
		}

		public static class TenantManagement
		{
			public const string AllowSelfRegistration = "App.TenantManagement.AllowSelfRegistration";

			public const string IsNewRegisteredTenantActiveByDefault = "App.TenantManagement.IsNewRegisteredTenantActiveByDefault";

			public const string UseCaptchaOnRegistration = "App.TenantManagement.UseCaptchaOnRegistration";

			public const string DefaultEdition = "App.TenantManagement.DefaultEdition";

			public const string RequireApprovalForNewTenants = "App.TenantManagement.RequireApprovalForNewTenants";

			public const string HostLoginMode = "App.TenantManagement.HostLoginMode";
		}

		public static class HostLoginModes
		{
			public const string LoginPage = "LoginPage";

			public const string SeparatePage = "SeparatePage";
		}

		public static class UserManagement
		{
			public const string AllowSelfRegistration = "App.UserManagement.AllowSelfRegistration";

			public const string IsNewRegisteredUserActiveByDefault = "App.UserManagement.IsNewRegisteredUserActiveByDefault";

			public const string UseCaptchaOnRegistration = "App.UserManagement.UseCaptchaOnRegistration";
		}

		public static class Security
		{
			public const string PasswordComplexity = "App.Security.PasswordComplexity";
		}

		public static class Subscription
		{
			public const string GraceDays = "App.Subscription.GraceDays";

			public const string WarnBeforeDays = "App.Subscription.WarnBeforeDays";
		}

		public static class Ecomm
		{
			public const string WillUseServerImage = "App.Ecomm.WillUseServerImage";

			public const string WillUseCloudinaryImage = "App.Ecomm.WillUseCloudinaryImage";

			public const string WebSiteAddress = "App.Ecomm.WebSiteAddress";

			public const string ImageServerUrl = "App.Ecomm.ImageServerUrl";
		}
	}
}
