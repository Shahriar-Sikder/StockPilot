using System;
using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class ItemLedgerReportDto
	{
		public int ItemId { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string BaseUnitCode { get; set; }

		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public decimal OpeningQuantity { get; set; }

		public decimal OpeningValue { get; set; }

		public decimal TotalIn { get; set; }

		public decimal TotalInValue { get; set; }

		public decimal TotalOut { get; set; }

		public decimal TotalOutValue { get; set; }

		public decimal ClosingQuantity { get; set; }

		public decimal ClosingValue { get; set; }

		public List<ItemLedgerRowDto> Rows { get; set; } = new List<ItemLedgerRowDto>();
	}
}
