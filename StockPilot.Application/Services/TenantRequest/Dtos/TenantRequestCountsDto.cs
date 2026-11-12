namespace StockPilot.Services.TenantRequest.Dtos
{
	public class TenantRequestCountsDto
	{
		public int Pending { get; set; }

		public int Approved { get; set; }

		public int Rejected { get; set; }

		public int Cancelled { get; set; }

		public int Total => Pending + Approved + Rejected + Cancelled;
	}
}
