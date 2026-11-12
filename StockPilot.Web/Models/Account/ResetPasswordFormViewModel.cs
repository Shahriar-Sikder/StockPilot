using System.ComponentModel.DataAnnotations;
using Abp.Auditing;

namespace StockPilot.Web.Models.Account
{
	public class ResetPasswordFormViewModel
	{
		public string TenantId { get; set; }

		[Required]
		public string UserId { get; set; }

		[Required]
		public string ResetCode { get; set; }

		[Required]
		[DisableAuditing]
		public string Password { get; set; }
	}
}
