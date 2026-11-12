using System;
using System.Collections.Generic;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class DashboardSummaryDto
	{
		public decimal StockValue { get; set; }

		public int TrackedItemCount { get; set; }

		public DateTime PeriodFrom { get; set; }

		public DateTime PeriodTo { get; set; }

		public decimal StockInTotal { get; set; }

		public decimal StockOutTotal { get; set; }

		public List<DashboardDayDto> Days { get; set; } = new List<DashboardDayDto>();

		public DashboardHealthDto Health { get; set; } = new DashboardHealthDto();

		public int DraftCount { get; set; }

		public int LowStockCount { get; set; }

		public int AttentionCount => DraftCount + LowStockCount;

		public List<DashboardAttentionItemDto> Attention { get; set; } = new List<DashboardAttentionItemDto>();

		public List<DashboardMovementDto> RecentMovements { get; set; } = new List<DashboardMovementDto>();
	}
}
