using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.Net.Mail;
using StockPilot.Emailing;
using StockPilot.Web;

namespace StockPilot.MultiTenancy
{
	public class TenantRequestEmailer : StockPilotDomainServiceBase
	{
		private readonly IEmailSender _emailSender;

		private readonly IEmailTemplateProvider _emailTemplateProvider;

		private readonly IWebUrlService _webUrlService;

		public TenantRequestEmailer(IEmailSender emailSender, IEmailTemplateProvider emailTemplateProvider, IWebUrlService webUrlService)
		{
			_emailSender = emailSender;
			_emailTemplateProvider = emailTemplateProvider;
			_webUrlService = webUrlService;
		}

		public virtual Task SendReceivedAsync(TenantRequest request)
		{
			string statusLink = AppLink("#/register-status?requestNo=" + Uri.EscapeDataString(request.RequestNo));
			return SendAsync(request, L("TenantRequestReceivedEmail_Subject", request.RequestNo), L("TenantRequestReceivedEmail_Body", Html(request.AdminName), Html(request.StoreName), Html(request.RequestNo)), statusLink);
		}

		public virtual Task SendApprovedAsync(TenantRequest request, string tenancyName)
		{
			string loginLink = AppLink("#/login?tenancyName=" + Uri.EscapeDataString(tenancyName));
			return SendAsync(request, L("TenantRequestApprovedEmail_Subject", request.StoreName), L("TenantRequestApprovedEmail_Body", Html(request.AdminName), Html(request.StoreName), Html(tenancyName)), loginLink);
		}

		public virtual Task SendRejectedAsync(TenantRequest request)
		{
			return SendAsync(request, L("TenantRequestRejectedEmail_Subject", request.RequestNo), L("TenantRequestRejectedEmail_Body", Html(request.AdminName), Html(request.RequestNo), Html(request.StoreName), Html(request.ReviewNote)), null);
		}

		private async Task SendAsync(TenantRequest request, string subject, string body, string link)
		{
			string to = request.AdminEmailAddress;
			if (to.IsNullOrWhiteSpace())
			{
				return;
			}
			try
			{
				string smtpHost = await base.SettingManager.GetSettingValueForApplicationAsync("Abp.Net.Mail.Smtp.Host");
				string fromAddress = await base.SettingManager.GetSettingValueForApplicationAsync("Abp.Net.Mail.DefaultFromAddress");
				if (smtpHost.IsNullOrWhiteSpace() || fromAddress.IsNullOrWhiteSpace())
				{
					base.Logger.Info("Store request " + request.RequestNo + ": email is not configured (SMTP host and sender address), \"" + subject + "\" skipped.");
					return;
				}
				StringBuilder html = new StringBuilder(_emailTemplateProvider.GetDefaultTemplate(null));
				html.Replace("{EMAIL_TITLE}", WebUtility.HtmlEncode(subject));
				html.Replace("{EMAIL_SUB_TITLE}", WebUtility.HtmlEncode(request.StoreName ?? string.Empty));
				string message = body.Replace("\n", "<br />");
				if (!link.IsNullOrEmpty())
				{
					message = message + "<br /><br /><a href=\"" + WebUtility.HtmlEncode(link) + "\">" + WebUtility.HtmlEncode(link) + "</a>";
				}
				html.Replace("{EMAIL_BODY}", message);
				await _emailSender.SendAsync(to, subject, html.ToString());
			}
			catch (Exception exception)
			{
				base.Logger.Warn("Store request " + request.RequestNo + ": email \"" + subject + "\" could not be sent.", exception);
			}
		}

		private string AppLink(string hash)
		{
			return _webUrlService.GetSiteRootAddress() + "react/index.html" + hash;
		}

		private static string Html(string value)
		{
			return WebUtility.HtmlEncode(value ?? string.Empty);
		}
	}
}
