using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;
using StockPilot.Services.Common.Dtos;

namespace StockPilot.Services.ItemMaster.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.ItemMaster) })]
	public class CreateItemMasterDto : FullAuditedEntityDto
	{
		[Required]
		[StringLength(50)]
		public string ItemCode { get; set; }

		[Required]
		[StringLength(250)]
		public string ItemName { get; set; }

		[StringLength(1000)]
		public string Description { get; set; }

		[StringLength(100)]
		public string Barcode { get; set; }

		[Range(1, int.MaxValue)]
		public int ItemCategoryId { get; set; }

		public int? ItemSubCategoryId { get; set; }

		[Range(1, int.MaxValue)]
		public int UnitSetId { get; set; }

		[Range(1, int.MaxValue)]
		public int BaseUnitId { get; set; }

		public int? PurchaseUnitId { get; set; }

		public int? SalesUnitId { get; set; }

		public int? DefaultSupplierId { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal StandardCost { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal SalePrice { get; set; }

		[Range(0, 100)]
		public decimal TaxPercent { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal ReorderLevel { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal MinStock { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal MaxStock { get; set; }

		public bool IsStockItem { get; set; } = true;

		public bool IsActive { get; set; } = true;

		public PictureContentDto Picture { get; set; }
	}
}
