using System;
using Abp.AutoMapper;
using Abp.Notifications;

namespace StockPilot.Notifications.Dto
{
	[AutoMapFrom(new Type[] { typeof(NotificationDefinition) })]
	public class NotificationSubscriptionWithDisplayNameDto : NotificationSubscriptionDto
	{
		public string DisplayName { get; set; }

		public string Description { get; set; }
	}
}
