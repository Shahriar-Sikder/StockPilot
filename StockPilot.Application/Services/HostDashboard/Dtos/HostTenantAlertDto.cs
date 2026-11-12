using System;

namespace StockPilot.Services.HostDashboard.Dtos
{
	public class HostTenantAlertDto
	{
		public int TenantId { get; set; }

		public string TenancyName { get; set; }

		public string Name { get; set; }

		public DateTime? SubscriptionEndDate { get; set; }

		public int UserCount { get; set; }

		public int MaxUserCount { get; set; }

		public int ItemCount { get; set; }

		public int MaxItemCount { get; set; }
	}
}
