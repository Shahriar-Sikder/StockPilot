namespace StockPilot.Services.Dashboard.Dtos
{
	public class DashboardHealthDto
	{
		public int OkCount { get; set; }

		public int LowCount { get; set; }

		public int OutCount { get; set; }

		public int TotalCount => OkCount + LowCount + OutCount;
	}
}
