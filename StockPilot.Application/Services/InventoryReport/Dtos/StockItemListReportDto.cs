using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class StockItemListReportDto
	{
		public List<StockItemListRowDto> Rows { get; set; } = new List<StockItemListRowDto>();

		public int ItemCount { get; set; }

		public decimal TotalValue { get; set; }
	}
}
