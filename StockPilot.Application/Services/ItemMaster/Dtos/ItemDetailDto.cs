using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using StockPilot.Inventory;

namespace StockPilot.Services.ItemMaster.Dtos
{
	public class ItemDetailDto : EntityDto
	{
		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string Description { get; set; }

		public string Barcode { get; set; }

		public string ItemCategoryName { get; set; }

		public string ItemSubCategoryName { get; set; }

		public Guid? PictureId { get; set; }

		public bool IsStockItem { get; set; }

		public bool IsActive { get; set; }

		public ItemStockStatus Status { get; set; }

		public decimal QuantityOnHand { get; set; }

		public decimal ReservedQuantity { get; set; }

		public decimal AvailableQuantity { get; set; }

		public decimal AverageCost { get; set; }

		public decimal LastPurchaseCost { get; set; }

		public decimal StockValue { get; set; }

		public decimal StandardCost { get; set; }

		public decimal SalePrice { get; set; }

		public decimal ReorderLevel { get; set; }

		public decimal LevelRatio { get; set; }

		public int BaseUnitId { get; set; }

		public string BaseUnitCode { get; set; }

		public string BaseUnitName { get; set; }

		public List<ItemUnitDto> Units { get; set; } = new List<ItemUnitDto>();

		public List<ItemMovementDto> RecentMovements { get; set; } = new List<ItemMovementDto>();

		public DateTime CreationTime { get; set; }

		public DateTime? LastModificationTime { get; set; }
	}
}
