using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	public class UploadPurchaseDocumentInput
	{
		[Range(1, int.MaxValue)]
		public int PurchaseInvoiceId { get; set; }

		[Required]
		[StringLength(255)]
		public string FileName { get; set; }

		[Required]
		public string Base64Content { get; set; }
	}
}
