using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class SalesListReportDto
	{
		public List<SalesListRowDto> Rows { get; set; } = new List<SalesListRowDto>();

		public int DocumentCount { get; set; }

		public int PostedCount { get; set; }

		public decimal PostedSales { get; set; }

		public decimal PostedCostOfSales { get; set; }

		public decimal PostedMargin { get; set; }

		public decimal? PostedMarginPercent { get; set; }

		public decimal PostedIssueCost { get; set; }
	}
}
