using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class SalesListInput
	{
		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public IssueSaleType? IssueType { get; set; }

		public string CustomerName { get; set; }

		public DocumentStatus? Status { get; set; }
	}
}
