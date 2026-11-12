using Abp.Auditing;

namespace StockPilot.Configuration.Host.Dto
{
	public class EmailSettingsEditDto
	{
		public string DefaultFromAddress { get; set; }

		public string DefaultFromDisplayName { get; set; }

		public string SmtpHost { get; set; }

		public int SmtpPort { get; set; }

		public string SmtpUserName { get; set; }

		[DisableAuditing]
		public string SmtpPassword { get; set; }

		public string SmtpDomain { get; set; }

		public bool SmtpEnableSsl { get; set; }

		public bool SmtpUseDefaultCredentials { get; set; }
	}
}
