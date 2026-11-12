using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceListReportDto
	{
		public List<InvoiceListRowDto> Rows { get; set; } = new List<InvoiceListRowDto>();

		public int InvoiceCount { get; set; }

		public int PostedCount { get; set; }

		public decimal PostedTotal { get; set; }

		public decimal DraftTotal { get; set; }
	}
}
