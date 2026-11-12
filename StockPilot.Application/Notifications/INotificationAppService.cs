using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Notifications.Dto;

namespace StockPilot.Notifications
{
	public interface INotificationAppService : IApplicationService, ITransientDependency
	{
		Task<GetNotificationsOutput> GetUserNotifications(GetUserNotificationsInput input);

		Task SetAllNotificationsAsRead();

		Task SetNotificationAsRead(EntityDto<Guid> input);

		Task<GetNotificationSettingsOutput> GetNotificationSettings();

		Task UpdateNotificationSettings(UpdateNotificationSettingsInput input);
	}
}
