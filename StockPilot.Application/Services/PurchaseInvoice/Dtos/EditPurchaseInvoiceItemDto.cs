using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	[AutoMap(new Type[] { typeof(PurchaseInvoiceItem) })]
	public class EditPurchaseInvoiceItemDto : CreationAuditedEntityDto
	{
		[Range(1, int.MaxValue)]
		public int ItemMasterId { get; set; }

		[Range(1, int.MaxValue)]
		public int UnitMasterId { get; set; }

		[Range(typeof(decimal), "0.0001", "999999999999")]
		public decimal Quantity { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal UnitCost { get; set; }

		[Range(0, 100)]
		public decimal DiscountPercent { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal DiscountAmount { get; set; }

		[Range(0, 100)]
		public decimal TaxPercent { get; set; }

		public DateTime? ExpiryDate { get; set; }

		[StringLength(500)]
		public string Remarks { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string UnitCode { get; set; }

		public string BaseUnitCode { get; set; }

		public decimal ConversionFactor { get; set; }

		public decimal BaseQuantity { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal LineTotal { get; set; }
	}
}
