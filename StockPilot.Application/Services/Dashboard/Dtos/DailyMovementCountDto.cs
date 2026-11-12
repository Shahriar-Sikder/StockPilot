using System;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class DailyMovementCountDto
	{
		public DateTime Date { get; set; }

		public int InCount { get; set; }

		public int OutCount { get; set; }
	}
}
