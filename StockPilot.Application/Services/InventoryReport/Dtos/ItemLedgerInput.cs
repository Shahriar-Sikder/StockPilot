using System;
using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class ItemLedgerInput
	{
		[Range(1, int.MaxValue)]
		public int ItemMasterId { get; set; }

		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }
	}
}
