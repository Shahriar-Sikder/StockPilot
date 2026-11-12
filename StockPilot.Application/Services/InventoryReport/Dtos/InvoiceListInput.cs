using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceListInput
	{
		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public int? SupplierId { get; set; }

		public DocumentStatus? Status { get; set; }
	}
}
