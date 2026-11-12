namespace StockPilot.MultiTenancy.Dto
{
	public class TenantFeatureUsageDto
	{
		public int UserCount { get; set; }

		public int MaxUserCount { get; set; }

		public int ActiveItemCount { get; set; }

		public int MaxItemCount { get; set; }
	}
}
