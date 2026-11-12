using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceListRowDto
	{
		public int InvoiceId { get; set; }

		public string InvoiceNo { get; set; }

		public DateTime InvoiceDate { get; set; }

		public string SupplierName { get; set; }

		public string SupplierInvoiceNo { get; set; }

		public string CurrencyCode { get; set; }

		public decimal ExchangeRate { get; set; }

		public int LineCount { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal OtherCharges { get; set; }

		public decimal TotalAmount { get; set; }

		public decimal TotalInBaseCurrency { get; set; }

		public DocumentStatus Status { get; set; }
	}
}
