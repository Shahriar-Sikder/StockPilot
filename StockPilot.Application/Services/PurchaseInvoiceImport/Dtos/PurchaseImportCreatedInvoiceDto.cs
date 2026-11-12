using Abp.Application.Services.Dto;

namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class PurchaseImportCreatedInvoiceDto : EntityDto
	{
		public string InvoiceNo { get; set; }

		public bool IsPosted { get; set; }
	}
}
