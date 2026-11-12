namespace StockPilot.Inventory
{
	public class FifoAllocation
	{
		public long? InMovementId { get; set; }

		public long OutMovementId { get; set; }

		public decimal Quantity { get; set; }
	}
}
