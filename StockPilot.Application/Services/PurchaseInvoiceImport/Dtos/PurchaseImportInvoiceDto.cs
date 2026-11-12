using System;
using System.Collections.Generic;

namespace StockPilot.Services.PurchaseInvoiceImport.Dtos
{
	public class PurchaseImportInvoiceDto
	{
		public string InvoiceNo { get; set; }

		public DateTime? InvoiceDate { get; set; }

		public int? SupplierId { get; set; }

		public string SupplierName { get; set; }

		public string CurrencyCode { get; set; }

		public int LineCount { get; set; }

		public decimal Total { get; set; }

		public PurchaseImportStatus Status { get; set; }

		public List<PurchaseImportMessageDto> Messages { get; set; } = new List<PurchaseImportMessageDto>();
	}
}
