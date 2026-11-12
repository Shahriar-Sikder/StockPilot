using System.ComponentModel.DataAnnotations;

namespace StockPilot.Web.Models.Account
{
	public class EmailConfirmationViewModel
	{
		public string TenantId { get; set; }

		[Required]
		public string UserId { get; set; }

		[Required]
		public string ConfirmationCode { get; set; }
	}
}
