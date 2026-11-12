using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Notifications;
using Abp.Runtime.Session;
using StockPilot.Notifications.Dto;

namespace StockPilot.Notifications
{
	[AbpAuthorize(new string[] { })]
	public class NotificationAppService : StockPilotAppServiceBase, INotificationAppService, IApplicationService, ITransientDependency
	{
		private readonly INotificationDefinitionManager _notificationDefinitionManager;

		private readonly IUserNotificationManager _userNotificationManager;

		private readonly INotificationSubscriptionManager _notificationSubscriptionManager;

		public NotificationAppService(INotificationDefinitionManager notificationDefinitionManager, IUserNotificationManager userNotificationManager, INotificationSubscriptionManager notificationSubscriptionManager)
		{
			_notificationDefinitionManager = notificationDefinitionManager;
			_userNotificationManager = userNotificationManager;
			_notificationSubscriptionManager = notificationSubscriptionManager;
		}

		[DisableAuditing]
		public async Task<GetNotificationsOutput> GetUserNotifications(GetUserNotificationsInput input)
		{
			return new GetNotificationsOutput(await _userNotificationManager.GetUserNotificationCountAsync(base.AbpSession.ToUserIdentifier(), input.State), await _userNotificationManager.GetUserNotificationCountAsync(base.AbpSession.ToUserIdentifier(), UserNotificationState.Unread), await _userNotificationManager.GetUserNotificationsAsync(base.AbpSession.ToUserIdentifier(), input.State, input.SkipCount, input.MaxResultCount));
		}

		public async Task SetAllNotificationsAsRead()
		{
			await _userNotificationManager.UpdateAllUserNotificationStatesAsync(base.AbpSession.ToUserIdentifier(), UserNotificationState.Read);
		}

		public async Task SetNotificationAsRead(EntityDto<Guid> input)
		{
			if ((await _userNotificationManager.GetUserNotificationAsync(base.AbpSession.TenantId, input.Id)).UserId != base.AbpSession.GetUserId())
			{
				throw new ApplicationException($"Given user notification id ({input.Id}) is not belong to the current user ({base.AbpSession.GetUserId()})");
			}
			await _userNotificationManager.UpdateUserNotificationStateAsync(base.AbpSession.TenantId, input.Id, UserNotificationState.Read);
		}

		public async Task<GetNotificationSettingsOutput> GetNotificationSettings()
		{
			GetNotificationSettingsOutput output = new GetNotificationSettingsOutput();
			GetNotificationSettingsOutput getNotificationSettingsOutput = output;
			getNotificationSettingsOutput.ReceiveNotifications = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Notifications.ReceiveNotifications");
			getNotificationSettingsOutput = output;
			getNotificationSettingsOutput.Notifications = (await _notificationDefinitionManager.GetAllAvailableAsync(base.AbpSession.ToUserIdentifier())).Where((NotificationDefinition nd) => nd.EntityType == null).MapTo<List<NotificationSubscriptionWithDisplayNameDto>>();
			List<string> subscribedNotifications = (await _notificationSubscriptionManager.GetSubscribedNotificationsAsync(base.AbpSession.ToUserIdentifier())).Select((NotificationSubscription ns) => ns.NotificationName).ToList();
			output.Notifications.ForEach(delegate(NotificationSubscriptionWithDisplayNameDto n)
			{
				n.IsSubscribed = subscribedNotifications.Contains(n.Name);
			});
			return output;
		}

		public async Task UpdateNotificationSettings(UpdateNotificationSettingsInput input)
		{
			await base.SettingManager.ChangeSettingForUserAsync(base.AbpSession.ToUserIdentifier(), "Abp.Notifications.ReceiveNotifications", input.ReceiveNotifications.ToString());
			foreach (NotificationSubscriptionDto notification in input.Notifications)
			{
				if (notification.IsSubscribed)
				{
					await _notificationSubscriptionManager.SubscribeAsync(base.AbpSession.ToUserIdentifier(), notification.Name);
				}
				else
				{
					await _notificationSubscriptionManager.UnsubscribeAsync(base.AbpSession.ToUserIdentifier(), notification.Name);
				}
			}
		}
	}
}
