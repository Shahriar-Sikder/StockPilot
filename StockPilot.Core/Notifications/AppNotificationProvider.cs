using Abp.Authorization;
using Abp.Localization;
using Abp.Notifications;

namespace StockPilot.Notifications
{
	public class AppNotificationProvider : NotificationProvider
	{
		public override void SetNotifications(INotificationDefinitionContext context)
		{
			context.Manager.Add(new NotificationDefinition("App.NewUserRegistered", null, L("NewUserRegisteredNotificationDefinition"), null, new SimplePermissionDependency("Pages.Administration.Users")));
			context.Manager.Add(new NotificationDefinition("App.NewTenantRegistered", null, L("NewTenantRegisteredNotificationDefinition"), null, new SimplePermissionDependency("Pages.Tenants")));
			context.Manager.Add(new NotificationDefinition("App.NewTenantRequest", null, L("NewTenantRequestNotificationDefinition"), null, new SimplePermissionDependency("Pages.Tenants.Requests")));
			context.Manager.Add(new NotificationDefinition("App.OrderReceivedMessage", null, L("OrderReceivedMessageNotification")));
		}

		private static ILocalizableString L(string name)
		{
			return new LocalizableString(name, "StockPilot");
		}
	}
}
