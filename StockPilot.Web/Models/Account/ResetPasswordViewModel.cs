using System.ComponentModel.DataAnnotations;
using StockPilot.Security;

namespace StockPilot.Web.Models.Account
{
	public class ResetPasswordViewModel
	{
		public string TenantId { get; set; }

		[Required]
		public string UserId { get; set; }

		[Required]
		public string ResetCode { get; set; }

		public PasswordComplexitySetting PasswordComplexitySetting { get; set; }
	}
}
