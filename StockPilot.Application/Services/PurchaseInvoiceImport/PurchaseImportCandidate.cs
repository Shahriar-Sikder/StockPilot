using StockPilot.Services.PurchaseInvoice.Dtos;
using StockPilot.Services.PurchaseInvoiceImport.Dtos;

namespace StockPilot.Services.PurchaseInvoiceImport
{
	public class PurchaseImportCandidate
	{
		public PurchaseImportInvoiceDto Preview { get; }

		public CreatePurchaseInvoiceDto Invoice { get; }

		public int? CurrencyId { get; set; }

		public bool HasErrors => Preview.Messages.Count > 0;

		public PurchaseImportCandidate(string invoiceNo)
		{
			Preview = new PurchaseImportInvoiceDto
			{
				InvoiceNo = invoiceNo
			};
			Invoice = new CreatePurchaseInvoiceDto
			{
				InvoiceNo = invoiceNo
			};
		}

		public void AddError(int? rowNumber, string message)
		{
			Preview.Messages.Add(new PurchaseImportMessageDto
			{
				RowNumber = rowNumber,
				Message = message
			});
		}
	}
}
