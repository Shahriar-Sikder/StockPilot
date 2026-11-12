using System.ComponentModel.DataAnnotations;

namespace StockPilot.Configuration.Host.Dto
{
	public class SendTestEmailInput
	{
		[Required]
		[MaxLength(256)]
		public string EmailAddress { get; set; }
	}
}
