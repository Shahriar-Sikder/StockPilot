using System;
using System.Text;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Net.Mail;
using Abp.Runtime.Security;
using StockPilot.Emailing;
using StockPilot.MultiTenancy;
using StockPilot.Web;

namespace StockPilot.Authorization.Users
{
	public class UserEmailer : StockPilotServiceBase, IUserEmailer, ITransientDependency
	{
		private readonly IEmailTemplateProvider _emailTemplateProvider;

		private readonly IEmailSender _emailSender;

		private readonly IWebUrlService _webUrlService;

		private readonly IRepository<Tenant> _tenantRepository;

		private readonly ICurrentUnitOfWorkProvider _unitOfWorkProvider;

		public UserEmailer(IEmailTemplateProvider emailTemplateProvider, IEmailSender emailSender, IWebUrlService webUrlService, IRepository<Tenant> tenantRepository, ICurrentUnitOfWorkProvider unitOfWorkProvider)
		{
			_emailTemplateProvider = emailTemplateProvider;
			_emailSender = emailSender;
			_webUrlService = webUrlService;
			_tenantRepository = tenantRepository;
			_unitOfWorkProvider = unitOfWorkProvider;
		}

		[UnitOfWork]
		public virtual async Task SendEmailActivationLinkAsync(User user, string plainPassword = null)
		{
			if (user.EmailConfirmationCode.IsNullOrEmpty())
			{
				throw new ApplicationException("EmailConfirmationCode should be set in order to send email activation link.");
			}
			string tenancyName = GetTenancyNameOrNull(user.TenantId);
			string link = _webUrlService.GetSiteRootAddress(tenancyName) + "Account/EmailConfirmation?userId=" + Uri.EscapeDataString(SimpleStringCipher.Instance.Encrypt(user.Id.ToString())) + "&tenantId=" + ((!user.TenantId.HasValue) ? "" : Uri.EscapeDataString(SimpleStringCipher.Instance.Encrypt(user.TenantId.Value.ToString()))) + "&confirmationCode=" + Uri.EscapeDataString(user.EmailConfirmationCode);
			StringBuilder emailTemplate = new StringBuilder(_emailTemplateProvider.GetDefaultTemplate(user.TenantId));
			emailTemplate.Replace("{EMAIL_TITLE}", L("EmailActivation_Title"));
			emailTemplate.Replace("{EMAIL_SUB_TITLE}", L("EmailActivation_SubTitle"));
			StringBuilder mailMessage = new StringBuilder();
			mailMessage.AppendLine("<b>" + L("NameSurname") + "</b>: " + user.Name + " " + user.Surname + "<br />");
			if (!tenancyName.IsNullOrEmpty())
			{
				mailMessage.AppendLine("<b>" + L("TenancyName") + "</b>: " + tenancyName + "<br />");
			}
			mailMessage.AppendLine("<b>" + L("UserName") + "</b>: " + user.UserName + "<br />");
			if (!plainPassword.IsNullOrEmpty())
			{
				mailMessage.AppendLine("<b>" + L("Password") + "</b>: " + plainPassword + "<br />");
			}
			mailMessage.AppendLine("<br />");
			mailMessage.AppendLine(L("EmailActivation_ClickTheLinkBelowToVerifyYourEmail") + "<br /><br />");
			mailMessage.AppendLine("<a href=\"" + link + "\">" + link + "</a>");
			emailTemplate.Replace("{EMAIL_BODY}", mailMessage.ToString());
			await _emailSender.SendAsync(user.EmailAddress, L("EmailActivation_Subject"), emailTemplate.ToString());
		}

		public async Task SendPasswordResetLinkAsync(User user)
		{
			if (user.PasswordResetCode.IsNullOrEmpty())
			{
				throw new ApplicationException("PasswordResetCode should be set in order to send password reset link.");
			}
			string tenancyName = GetTenancyNameOrNull(user.TenantId);
			string link = _webUrlService.GetSiteRootAddress(tenancyName) + "Account/ResetPassword?userId=" + Uri.EscapeDataString(SimpleStringCipher.Instance.Encrypt(user.Id.ToString())) + "&tenantId=" + ((!user.TenantId.HasValue) ? "" : Uri.EscapeDataString(SimpleStringCipher.Instance.Encrypt(user.TenantId.Value.ToString()))) + "&resetCode=" + Uri.EscapeDataString(user.PasswordResetCode);
			StringBuilder emailTemplate = new StringBuilder(_emailTemplateProvider.GetDefaultTemplate(user.TenantId));
			emailTemplate.Replace("{EMAIL_TITLE}", L("PasswordResetEmail_Title"));
			emailTemplate.Replace("{EMAIL_SUB_TITLE}", L("PasswordResetEmail_SubTitle"));
			StringBuilder mailMessage = new StringBuilder();
			mailMessage.AppendLine("<b>" + L("NameSurname") + "</b>: " + user.Name + " " + user.Surname + "<br />");
			if (!tenancyName.IsNullOrEmpty())
			{
				mailMessage.AppendLine("<b>" + L("TenancyName") + "</b>: " + tenancyName + "<br />");
			}
			mailMessage.AppendLine("<b>" + L("UserName") + "</b>: " + user.UserName + "<br />");
			mailMessage.AppendLine("<br />");
			mailMessage.AppendLine(L("PasswordResetEmail_ClickTheLinkBelowToResetYourPassword") + "<br /><br />");
			mailMessage.AppendLine("<a href=\"" + link + "\">" + link + "</a>");
			emailTemplate.Replace("{EMAIL_BODY}", mailMessage.ToString());
			await _emailSender.SendAsync(user.EmailAddress, L("PasswordResetEmail_Subject"), emailTemplate.ToString());
		}

		private string GetTenancyNameOrNull(int? tenantId)
		{
			if (!tenantId.HasValue)
			{
				return null;
			}
			using (_unitOfWorkProvider.Current.SetTenantId(null))
			{
				return _tenantRepository.Get(tenantId.Value).TenancyName;
			}
		}
	}
}
