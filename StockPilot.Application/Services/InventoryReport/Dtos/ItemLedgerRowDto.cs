using System;
using StockPilot.Entity;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class ItemLedgerRowDto
	{
		public long MovementId { get; set; }

		public DateTime MovementDate { get; set; }

		public StockMovementType MovementType { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public string ReferenceNo { get; set; }

		public decimal QuantityIn { get; set; }

		public decimal QuantityOut { get; set; }

		public decimal UnitCost { get; set; }

		public decimal Value { get; set; }

		public decimal RunningBalance { get; set; }

		public string Remarks { get; set; }
	}
}
