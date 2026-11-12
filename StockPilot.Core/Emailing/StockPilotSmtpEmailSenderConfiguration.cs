using Abp.Configuration;
using Abp.Net.Mail.Smtp;
using Abp.Runtime.Security;

namespace StockPilot.Emailing
{
	public class StockPilotSmtpEmailSenderConfiguration : SmtpEmailSenderConfiguration
	{
		public override string Password => SimpleStringCipher.Instance.Decrypt(GetNotEmptySettingValue("Abp.Net.Mail.Smtp.Password"));

		public StockPilotSmtpEmailSenderConfiguration(ISettingManager settingManager)
			: base(settingManager)
		{
		}
	}
}
