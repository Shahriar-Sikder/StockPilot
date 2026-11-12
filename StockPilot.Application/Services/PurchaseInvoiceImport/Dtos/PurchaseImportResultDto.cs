using System.Collections.Generic;

namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class PurchaseImportResultDto
	{
		public List<PurchaseImportCreatedInvoiceDto> Created { get; set; } = new List<PurchaseImportCreatedInvoiceDto>();

		public List<PurchaseImportInvoiceDto> Skipped { get; set; } = new List<PurchaseImportInvoiceDto>();
	}
}
