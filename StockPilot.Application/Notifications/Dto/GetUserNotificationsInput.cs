using Abp.Notifications;
using StockPilot.Dto;

namespace StockPilot.Notifications.Dto
{
	public class GetUserNotificationsInput : PagedInputDto
	{
		public UserNotificationState? State { get; set; }
	}
}
