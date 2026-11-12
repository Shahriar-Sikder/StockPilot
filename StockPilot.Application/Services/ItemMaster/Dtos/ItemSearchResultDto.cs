using System;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.ItemMaster.Dtos
{
	public class ItemSearchResultDto : EntityDto
	{
		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string Barcode { get; set; }

		public Guid? PictureId { get; set; }

		public bool IsStockItem { get; set; }

		public decimal QuantityOnHand { get; set; }

		public decimal AvailableQuantity { get; set; }

		public int BaseUnitId { get; set; }

		public string BaseUnitCode { get; set; }

		public int? PurchaseUnitId { get; set; }

		public int? SalesUnitId { get; set; }

		public decimal StandardCost { get; set; }

		public decimal LastPurchaseCost { get; set; }

		public decimal SalePrice { get; set; }

		public decimal TaxPercent { get; set; }
	}
}
