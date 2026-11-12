using System.Collections.Generic;
using Abp.Application.Navigation;
using Abp.Localization;
using StockPilot.Sessions.Dto;

namespace StockPilot.Web.Models.Layout
{
	public class HeaderViewModel
	{
		public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

		public IReadOnlyList<LanguageInfo> Languages { get; set; }

		public LanguageInfo CurrentLanguage { get; set; }

		public UserMenu Menu { get; set; }

		public string CurrentPageName { get; set; }

		public bool IsMultiTenancyEnabled { get; set; }

		public bool TenantRegistrationEnabled { get; set; }

		public string GetShownLoginName()
		{
			if (!IsMultiTenancyEnabled)
			{
				return LoginInformations.User.UserName;
			}
			if (LoginInformations.Tenant != null)
			{
				return LoginInformations.Tenant.TenancyName + "\\" + LoginInformations.User.UserName;
			}
			return ".\\" + LoginInformations.User.UserName;
		}
	}
}
