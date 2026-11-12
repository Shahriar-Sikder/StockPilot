using System.Collections.Generic;
using StockPilot.Services.TenantRequest.Dtos;

namespace StockPilot.Services.HostDashboard.Dtos
{
	public class HostDashboardDto
	{
		public int ActiveTenants { get; set; }

		public int InactiveTenants { get; set; }

		public int PendingRequests { get; set; }

		public int ExpiringSoon { get; set; }

		public int WarnBeforeDays { get; set; }

		public List<TenantRequestListDto> LatestPendingRequests { get; set; } = new List<TenantRequestListDto>();

		public List<HostTenantAlertDto> TenantsExpiringSoon { get; set; } = new List<HostTenantAlertDto>();

		public List<HostTenantAlertDto> TenantsNearLimit { get; set; } = new List<HostTenantAlertDto>();
	}
}
