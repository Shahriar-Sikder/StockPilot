using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;
using StockPilot.Inventory;

namespace StockPilot.Services.ItemMaster.Dtos
{
	[AutoMapFrom(new Type[]
	{
		typeof(StockPilot.Entity.ItemMaster),
		typeof(ItemMasterGridRow)
	})]
	public class ItemMasterListDto : EntityDto, IHasCreationTime
	{
		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string Barcode { get; set; }

		public int ItemCategoryId { get; set; }

		public string ItemCategoryName { get; set; }

		public int? ItemSubCategoryId { get; set; }

		public string ItemSubCategoryName { get; set; }

		public int BaseUnitId { get; set; }

		public string BaseUnitCode { get; set; }

		public string PictureUrl { get; set; }

		public decimal SalePrice { get; set; }

		public decimal ReorderLevel { get; set; }

		public decimal QuantityOnHand { get; set; }

		public decimal AverageCost { get; set; }

		public decimal StockValue { get; set; }

		public ItemStockStatus Status { get; set; }

		public decimal LevelRatio { get; set; }

		public bool IsStockItem { get; set; }

		public bool IsActive { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
