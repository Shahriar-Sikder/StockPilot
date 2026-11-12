using System.ComponentModel.DataAnnotations;
using Abp.Auditing;

namespace StockPilot.WebApi.Models
{
	public class LoginModel
	{
		public string TenancyName { get; set; }

		[Required]
		public string UsernameOrEmailAddress { get; set; }

		[Required]
		[DisableAuditing]
		public string Password { get; set; }

		public bool HostLogin { get; set; }
	}
}
