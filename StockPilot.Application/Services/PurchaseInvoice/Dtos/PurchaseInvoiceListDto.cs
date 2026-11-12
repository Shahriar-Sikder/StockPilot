using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	[AutoMapFrom(new Type[]
	{
		typeof(StockPilot.Entity.PurchaseInvoice),
		typeof(PurchaseInvoiceGridRow)
	})]
	public class PurchaseInvoiceListDto : EntityDto, IHasCreationTime
	{
		public string InvoiceNo { get; set; }

		public DateTime InvoiceDate { get; set; }

		public int SupplierId { get; set; }

		public string SupplierName { get; set; }

		public string SupplierInvoiceNo { get; set; }

		public string CurrencyCode { get; set; }

		public decimal ExchangeRate { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal OtherCharges { get; set; }

		public decimal TotalAmount { get; set; }

		public DocumentStatus Status { get; set; }

		public DateTime? PostedTime { get; set; }

		public int LineCount { get; set; }

		public int AttachmentCount { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
