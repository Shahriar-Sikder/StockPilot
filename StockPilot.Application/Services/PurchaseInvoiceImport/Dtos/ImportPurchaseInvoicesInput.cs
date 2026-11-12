namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class ImportPurchaseInvoicesInput : PurchaseImportFileInput
	{
		public bool PostAfterImport { get; set; }
	}
}
