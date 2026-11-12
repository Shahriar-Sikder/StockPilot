namespace StockPilot.Configuration.Host.Dto
{
	public class TenantManagementSettingsEditDto
	{
		public bool AllowSelfRegistration { get; set; }

		public bool IsNewRegisteredTenantActiveByDefault { get; set; }

		public bool UseCaptchaOnRegistration { get; set; }

		public int? DefaultEditionId { get; set; }

		public bool RequireApprovalForNewTenants { get; set; }

		public string HostLoginMode { get; set; }
	}
}
