using System;
using StockPilot.Dto;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	public class PurchaseInvoiceGridInput : PagedSortedAndFilteredInputDto
	{
		public DocumentStatus? Status { get; set; }

		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public int? SupplierId { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
