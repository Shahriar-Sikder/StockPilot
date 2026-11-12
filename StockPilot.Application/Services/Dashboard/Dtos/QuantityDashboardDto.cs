using System;
using System.Collections.Generic;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class QuantityDashboardDto
	{
		public int TotalItems { get; set; }

		public DateTime PeriodFrom { get; set; }

		public DateTime PeriodTo { get; set; }

		public ItemQuantityDto MostIn { get; set; }

		public ItemQuantityDto MostOut { get; set; }

		public int DraftCount { get; set; }

		public int LowStockCount { get; set; }

		public int AttentionCount => DraftCount + LowStockCount;

		public List<AttentionItemDto> Attention { get; set; } = new List<AttentionItemDto>();

		public List<DailyMovementCountDto> InOutChart { get; set; } = new List<DailyMovementCountDto>();

		public DashboardHealthDto StockHealth { get; set; } = new DashboardHealthDto();

		public List<DashboardMovementDto> RecentActivity { get; set; } = new List<DashboardMovementDto>();

		public DateTime MonthFrom { get; set; }

		public List<TopItemDto> TopReceivedThisMonth { get; set; } = new List<TopItemDto>();

		public List<TopItemDto> TopSoldThisMonth { get; set; } = new List<TopItemDto>();

		public List<InsightDto> Insights { get; set; } = new List<InsightDto>();
	}
}
