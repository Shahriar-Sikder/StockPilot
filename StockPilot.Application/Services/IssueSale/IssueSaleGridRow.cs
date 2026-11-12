using System;
using StockPilot.Entity;

namespace StockPilot.Services.IssueSale
{
	public class IssueSaleGridRow
	{
		public int Id { get; set; }

		public string DocumentNo { get; set; }

		public DateTime DocumentDate { get; set; }

		public IssueSaleType IssueType { get; set; }

		public string CustomerName { get; set; }

		public string ReferenceNo { get; set; }

		public string CurrencyCode { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TaxAmount { get; set; }

		public decimal TotalAmount { get; set; }

		public decimal CostTotal { get; set; }

		public DocumentStatus Status { get; set; }

		public DateTime? PostedTime { get; set; }

		public int LineCount { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
