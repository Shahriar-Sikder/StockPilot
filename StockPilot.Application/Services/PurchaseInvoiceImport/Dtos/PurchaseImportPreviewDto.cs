using System.Collections.Generic;

namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class PurchaseImportPreviewDto
	{
		public int ReadyCount { get; set; }

		public int ExistsCount { get; set; }

		public int ErrorCount { get; set; }

		public List<PurchaseImportInvoiceDto> Invoices { get; set; } = new List<PurchaseImportInvoiceDto>();
	}
}
