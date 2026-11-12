using StockPilot.Inventory;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class StockItemListRowDto
	{
		public int ItemId { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string ItemCategoryName { get; set; }

		public string ItemSubCategoryName { get; set; }

		public string BaseUnitCode { get; set; }

		public decimal QuantityOnHand { get; set; }

		public decimal AverageCost { get; set; }

		public decimal StockValue { get; set; }

		public decimal ReorderLevel { get; set; }

		public ItemStockStatus Status { get; set; }
	}
}
