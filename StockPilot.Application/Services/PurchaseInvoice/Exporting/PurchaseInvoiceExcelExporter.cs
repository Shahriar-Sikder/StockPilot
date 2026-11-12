using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.PurchaseInvoice.Dtos;

namespace StockPilot.Services.PurchaseInvoice.Exporting
{
	public class PurchaseInvoiceExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<PurchaseInvoiceListDto> rows)
		{
			List<ExcelColumn<PurchaseInvoiceListDto>> columns = new List<ExcelColumn<PurchaseInvoiceListDto>>
			{
				InventoryExcelExporterBase.Col("ColNumber", (PurchaseInvoiceListDto r) => r.InvoiceNo),
				InventoryExcelExporterBase.Col("ColDate", (PurchaseInvoiceListDto r) => InventoryExcelExporterBase.DateOnly(r.InvoiceDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColSupplier", (PurchaseInvoiceListDto r) => r.SupplierName),
				InventoryExcelExporterBase.Col("ColSupplierInvoiceNo", (PurchaseInvoiceListDto r) => r.SupplierInvoiceNo),
				InventoryExcelExporterBase.Col("ColCurrency", (PurchaseInvoiceListDto r) => r.CurrencyCode),
				InventoryExcelExporterBase.Col("ColExchangeRate", (PurchaseInvoiceListDto r) => r.ExchangeRate, "#,##0.######"),
				InventoryExcelExporterBase.Col("ColLines", (PurchaseInvoiceListDto r) => r.LineCount),
				InventoryExcelExporterBase.Col("ColAttachments", (PurchaseInvoiceListDto r) => r.AttachmentCount),
				InventoryExcelExporterBase.CostCol("ColSubTotal", (PurchaseInvoiceListDto r) => r.SubTotal),
				InventoryExcelExporterBase.CostCol("ColDiscount", (PurchaseInvoiceListDto r) => r.DiscountAmount),
				InventoryExcelExporterBase.CostCol("ColTax", (PurchaseInvoiceListDto r) => r.TaxAmount),
				InventoryExcelExporterBase.CostCol("ColOtherCharges", (PurchaseInvoiceListDto r) => r.OtherCharges),
				InventoryExcelExporterBase.CostCol("ColTotal", (PurchaseInvoiceListDto r) => r.TotalAmount),
				InventoryExcelExporterBase.Col("ColStatus", (PurchaseInvoiceListDto r) => EnumText(r.Status)),
				InventoryExcelExporterBase.Col("ColPostedTime", (PurchaseInvoiceListDto r) => r.PostedTime, "yyyy-mm-dd hh:mm")
			};
			return Export("Purchases.xlsx", "Purchases", rows, columns);
		}
	}
}
