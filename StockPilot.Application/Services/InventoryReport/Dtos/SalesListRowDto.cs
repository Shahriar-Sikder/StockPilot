using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class SalesListRowDto
	{
		public int DocumentId { get; set; }

		public string DocumentNo { get; set; }

		public DateTime DocumentDate { get; set; }

		public IssueSaleType IssueType { get; set; }

		public string CustomerName { get; set; }

		public int LineCount { get; set; }

		public string CurrencyCode { get; set; }

		public decimal ExchangeRate { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal TotalAmount { get; set; }

		public decimal NetSalesInBaseCurrency { get; set; }

		public decimal Cost { get; set; }

		public decimal? Margin { get; set; }

		public decimal? MarginPercent { get; set; }

		public DocumentStatus Status { get; set; }
	}
}
