using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceToSalesReportDto
	{
		public string Note { get; set; }

		public List<InvoiceToSalesRowDto> Rows { get; set; } = new List<InvoiceToSalesRowDto>();

		public decimal TotalCostOfSold { get; set; }

		public decimal TotalSalesValue { get; set; }

		public decimal TotalMargin { get; set; }
	}
}
