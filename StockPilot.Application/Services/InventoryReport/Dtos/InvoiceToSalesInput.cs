using System;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceToSalesInput
	{
		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public int? SupplierId { get; set; }

		public int? ItemMasterId { get; set; }
	}
}
