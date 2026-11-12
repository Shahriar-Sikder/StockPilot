using System;
using StockPilot.Dto;
using StockPilot.MultiTenancy;

namespace StockPilot.Services.TenantRequest.Dtos
{
	public class TenantRequestGridInput : PagedSortedAndFilteredInputDto
	{
		public TenantRequestStatus? Status { get; set; }

		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }
	}
}
