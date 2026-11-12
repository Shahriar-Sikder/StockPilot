using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class FifoMovement
	{
		public long Id { get; set; }

		public StockMovementType MovementType { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitCost { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public int? ReferenceItemId { get; set; }
	}
}
