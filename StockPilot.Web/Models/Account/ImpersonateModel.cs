using System.ComponentModel.DataAnnotations;

namespace StockPilot.Web.Models.Account
{
	public class ImpersonateModel
	{
		public int? TenantId { get; set; }

		[Range(1.0, 9.223372036854776E+18)]
		public long UserId { get; set; }
	}
}
