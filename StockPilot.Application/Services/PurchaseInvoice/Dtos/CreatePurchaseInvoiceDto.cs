using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.PurchaseInvoice) })]
	public class CreatePurchaseInvoiceDto : FullAuditedEntityDto
	{
		[StringLength(50)]
		public string InvoiceNo { get; set; }

		public DateTime InvoiceDate { get; set; }

		[Range(1, int.MaxValue)]
		public int SupplierId { get; set; }

		[StringLength(50)]
		public string SupplierInvoiceNo { get; set; }

		public DateTime? DueDate { get; set; }

		[Range(1, int.MaxValue)]
		public int CurrencyId { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal ExchangeRate { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal OtherCharges { get; set; }

		[StringLength(1000)]
		public string Remarks { get; set; }

		public List<CreatePurchaseInvoiceItemDto> Lines { get; set; } = new List<CreatePurchaseInvoiceItemDto>();
	}
}
