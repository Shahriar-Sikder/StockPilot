using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.PurchaseInvoice) })]
	public class EditPurchaseInvoiceDto : CreationAuditedEntityDto
	{
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

		public List<EditPurchaseInvoiceItemDto> Lines { get; set; } = new List<EditPurchaseInvoiceItemDto>();

		public string InvoiceNo { get; set; }

		public string SupplierName { get; set; }

		public string CurrencyCode { get; set; }

		public DocumentStatus Status { get; set; }

		public bool IsPosted { get; set; }

		public DateTime? PostedTime { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal TotalAmount { get; set; }

		public List<PurchaseInvoiceDocumentDto> Attachments { get; set; } = new List<PurchaseInvoiceDocumentDto>();
	}
}
