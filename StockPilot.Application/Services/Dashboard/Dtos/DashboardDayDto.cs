using System;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class DashboardDayDto
	{
		public DateTime Date { get; set; }

		public decimal StockIn { get; set; }

		public decimal StockOut { get; set; }
	}
}
