using System;

namespace StockPilot.Inventory
{
	public class InsightItemData
	{
		public int ItemId { get; set; }

		public string ItemName { get; set; }

		public string BaseUnitCode { get; set; }

		public decimal OnHand { get; set; }

		public decimal ReorderLevel { get; set; }

		public decimal MaxStock { get; set; }

		public decimal OutQuantity { get; set; }

		public int OutMovementCount { get; set; }

		public decimal InQuantity { get; set; }

		public DateTime? LastOutDate { get; set; }

		public DateTime? FirstMovementDate { get; set; }
	}
}
