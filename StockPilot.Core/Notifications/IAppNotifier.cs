using System.Threading.Tasks;
using Abp;
using Abp.Notifications;
using StockPilot.Authorization.Users;
using StockPilot.MultiTenancy;

namespace StockPilot.Notifications
{
	public interface IAppNotifier
	{
		Task WelcomeToTheApplicationAsync(User user);

		Task NewUserRegisteredAsync(User user);

		Task NewTenantRegisteredAsync(Tenant tenant);

		Task NewTenantRequestAsync(TenantRequest request);

		Task SendMessageAsync(UserIdentifier user, string message, NotificationSeverity severity = NotificationSeverity.Info);

		Task NewOrderReceivedAsync(string orderId, UserIdentifier[] userIds, int tenantId);

		Task IndentPendingApprovalAsync(string indentNumber, long[] userIds, int tenantId);

		Task IndentApprovedAsync(string indentNumber, long userId, int tenantId);

		Task RequisitionPendingApprovalAsync(string requisitionNumber, long[] userIds, int tenantId);

		Task RequisitionApprovedAsync(string requisitionNumber, long userId, int tenantId);

		Task PurchaseOrderPendingApprovalAsync(string poNumber, long[] userIds, int tenantId);

		Task PurchaseOrderApprovedAsync(string poNumber, long userId, int tenantId);
	}
}
