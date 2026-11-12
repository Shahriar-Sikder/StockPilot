using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Localization;
using Abp.Notifications;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot.Notifications
{
	public class AppNotifier : StockPilotDomainServiceBase, IAppNotifier
	{
		private readonly INotificationPublisher _notificationPublisher;

		private readonly IRepository<User, long> _userRepository;

		private readonly IPermissionChecker _permissionChecker;

		public AppNotifier(INotificationPublisher notificationPublisher, IRepository<User, long> userRepository, IPermissionChecker permissionChecker)
		{
			_notificationPublisher = notificationPublisher;
			_userRepository = userRepository;
			_permissionChecker = permissionChecker;
		}

		public async Task NewTenantRequestAsync(TenantRequest request)
		{
			List<UserIdentifier> recipients = new List<UserIdentifier>();
			using (base.CurrentUnitOfWork.SetTenantId(null))
			{
				foreach (User user in await _userRepository.GetAllListAsync((User u) => u.TenantId == (int?)null && u.IsActive))
				{
					UserIdentifier identifier = new UserIdentifier(null, user.Id);
					if (await _permissionChecker.IsGrantedAsync(identifier, "Pages.Tenants.Requests"))
					{
						recipients.Add(identifier);
					}
				}
			}
			if (recipients.Any())
			{
				LocalizableMessageNotificationData notificationData = new LocalizableMessageNotificationData(new LocalizableString("NewTenantRequestNotificationMessage", "StockPilot"));
				notificationData["requestNo"] = request.RequestNo;
				notificationData["storeName"] = request.StoreName;
				notificationData["tenancyName"] = request.TenancyName;
				await _notificationPublisher.PublishAsync("App.NewTenantRequest", notificationData, null, NotificationSeverity.Info, recipients.ToArray());
			}
		}

		public async Task WelcomeToTheApplicationAsync(User user)
		{
			await _notificationPublisher.PublishAsync("App.WelcomeToTheApplication", new MessageNotificationData(L("WelcomeToTheApplicationNotificationMessage")), null, NotificationSeverity.Success, new UserIdentifier[1] { user.ToUserIdentifier() });
		}

		public async Task NewUserRegisteredAsync(User user)
		{
			LocalizableMessageNotificationData notificationData = new LocalizableMessageNotificationData(new LocalizableString("NewUserRegisteredNotificationMessage", "StockPilot"));
			notificationData["userName"] = user.UserName;
			notificationData["emailAddress"] = user.EmailAddress;
			await _notificationPublisher.PublishAsync("App.NewUserRegistered", notificationData, null, NotificationSeverity.Info, null, null, new int?[1] { user.TenantId });
		}

		public async Task NewTenantRegisteredAsync(Tenant tenant)
		{
			LocalizableMessageNotificationData notificationData = new LocalizableMessageNotificationData(new LocalizableString("NewTenantRegisteredNotificationMessage", "StockPilot"));
			notificationData["tenancyName"] = tenant.TenancyName;
			await _notificationPublisher.PublishAsync("App.NewTenantRegistered", notificationData);
		}

		public async Task SendMessageAsync(UserIdentifier user, string message, NotificationSeverity severity = NotificationSeverity.Info)
		{
			await _notificationPublisher.PublishAsync("App.SimpleMessage", new MessageNotificationData(message), null, severity, new UserIdentifier[1] { user });
		}

		public async Task NewOrderReceivedAsync(string orderNo, UserIdentifier[] userIds, int tenantId)
		{
			LocalizableMessageNotificationData notificationData = new LocalizableMessageNotificationData(new LocalizableString("OrderReceivedMessageNotificationDefinition", "StockPilot"));
			notificationData["orderNo"] = orderNo;
			await _notificationPublisher.PublishAsync("App.OrderReceivedMessage", notificationData, null, NotificationSeverity.Info, userIds);
		}

		public Task IndentPendingApprovalAsync(string indentNumber, long[] userIds, int tenantId)
		{
			return Task.CompletedTask;
		}

		public Task IndentApprovedAsync(string indentNumber, long userId, int tenantId)
		{
			return Task.CompletedTask;
		}

		public Task RequisitionPendingApprovalAsync(string requisitionNumber, long[] userIds, int tenantId)
		{
			return Task.CompletedTask;
		}

		public Task RequisitionApprovedAsync(string requisitionNumber, long userId, int tenantId)
		{
			return Task.CompletedTask;
		}

		public Task PurchaseOrderPendingApprovalAsync(string poNumber, long[] userIds, int tenantId)
		{
			return Task.CompletedTask;
		}

		public Task PurchaseOrderApprovedAsync(string poNumber, long userId, int tenantId)
		{
			return Task.CompletedTask;
		}
	}
}
