using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class PurchaseImportFileInput
	{
		[Required]
		[StringLength(255)]
		public string FileName { get; set; }

		[Required]
		public string Base64Content { get; set; }
	}
}
