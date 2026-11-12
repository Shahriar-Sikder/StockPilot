using StockPilot.Inventory;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class StockItemListInput
	{
		public int? ItemCategoryId { get; set; }

		public ItemStockStatus? Status { get; set; }

		public string Filter { get; set; }

		public bool IncludeInactive { get; set; } = true;
	}
}
