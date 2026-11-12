using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceToSalesAllocationDto
	{
		public long OutMovementId { get; set; }

		public StockMovementType MovementType { get; set; }

		public DateTime MovementDate { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public string ReferenceNo { get; set; }

		public decimal Quantity { get; set; }
	}
}
