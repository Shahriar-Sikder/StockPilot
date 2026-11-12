using System;

namespace StockPilot.Services.ItemMaster
{
	public class ItemMasterGridRow
	{
		public int Id { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string Barcode { get; set; }

		public int ItemCategoryId { get; set; }

		public string ItemCategoryName { get; set; }

		public int? ItemSubCategoryId { get; set; }

		public string ItemSubCategoryName { get; set; }

		public int BaseUnitId { get; set; }

		public string BaseUnitCode { get; set; }

		public Guid? PictureId { get; set; }

		public decimal SalePrice { get; set; }

		public decimal ReorderLevel { get; set; }

		public decimal QuantityOnHand { get; set; }

		public decimal AverageCost { get; set; }

		public decimal StockValue { get; set; }

		public int StatusCode { get; set; }

		public decimal LevelRatio { get; set; }

		public bool IsStockItem { get; set; }

		public bool IsActive { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
