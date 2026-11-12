using System.ComponentModel.DataAnnotations;

namespace StockPilot.Notifications.Dto
{
	public class NotificationSubscriptionDto
	{
		[Required]
		[MaxLength(96)]
		public string Name { get; set; }

		public bool IsSubscribed { get; set; }
	}
}
