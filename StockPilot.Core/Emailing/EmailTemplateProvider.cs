using System.IO;
using System.Reflection;
using System.Text;
using Abp.Dependency;
using Abp.Extensions;
using Abp.IO.Extensions;
using StockPilot.Web;

namespace StockPilot.Emailing
{
	public class EmailTemplateProvider : IEmailTemplateProvider, ITransientDependency
	{
		private readonly IWebUrlService _webUrlService;

		public EmailTemplateProvider(IWebUrlService webUrlService)
		{
			_webUrlService = webUrlService;
		}

		public string GetDefaultTemplate(int? tenantId)
		{
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StockPilot.Emailing.EmailTemplates.default.html"))
			{
				byte[] bytes = stream.GetAllBytes();
				return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Replace("{EMAIL_LOGO_URL}", GetTenantLogoUrl(tenantId));
			}
		}

		private string GetTenantLogoUrl(int? tenantId)
		{
			if (!tenantId.HasValue)
			{
				return _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "TenantCustomization/GetTenantLogo";
			}
			return _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "TenantCustomization/GetTenantLogo?tenantId=" + tenantId.Value;
		}
	}
}
