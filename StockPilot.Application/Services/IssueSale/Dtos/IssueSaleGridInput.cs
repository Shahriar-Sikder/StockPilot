using System;
using StockPilot.Dto;
using StockPilot.Entity;

namespace StockPilot.Services.IssueSale.Dtos
{
	public class IssueSaleGridInput : PagedSortedAndFilteredInputDto
	{
		public DocumentStatus? Status { get; set; }

		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public IssueSaleType? IssueType { get; set; }

		public string CustomerName { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
