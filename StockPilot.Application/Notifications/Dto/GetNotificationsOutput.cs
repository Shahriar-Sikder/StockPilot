using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Notifications;

namespace StockPilot.Notifications.Dto
{
	public class GetNotificationsOutput : PagedResultDto<UserNotification>
	{
		public int UnreadCount { get; set; }

		public GetNotificationsOutput(int totalCount, int unreadCount, List<UserNotification> notifications)
			: base(totalCount, (IReadOnlyList<UserNotification>)notifications)
		{
			UnreadCount = unreadCount;
		}
	}
}
