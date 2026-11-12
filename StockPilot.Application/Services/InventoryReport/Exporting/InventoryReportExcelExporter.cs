using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.InventoryReport.Dtos;

namespace StockPilot.Services.InventoryReport.Exporting
{
	public class InventoryReportExcelExporter : InventoryExcelExporterBase
	{
		public FileDto ExportStockItemList(StockItemListReportDto report)
		{
			List<ExcelColumn<StockItemListRowDto>> columns = new List<ExcelColumn<StockItemListRowDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (StockItemListRowDto r) => r.ItemCode),
				InventoryExcelExporterBase.Col("ColItem", (StockItemListRowDto r) => r.ItemName),
				InventoryExcelExporterBase.Col("ColCategory", (StockItemListRowDto r) => r.ItemCategoryName),
				InventoryExcelExporterBase.Col("ColSubCategory", (StockItemListRowDto r) => r.ItemSubCategoryName),
				InventoryExcelExporterBase.Col("ColUnit", (StockItemListRowDto r) => r.BaseUnitCode),
				InventoryExcelExporterBase.Col("ColOnHand", (StockItemListRowDto r) => r.QuantityOnHand, "#,##0.####"),
				InventoryExcelExporterBase.CostCol("ColAvgCost", (StockItemListRowDto r) => r.AverageCost),
				InventoryExcelExporterBase.CostCol("ColValue", (StockItemListRowDto r) => r.StockValue),
				InventoryExcelExporterBase.Col("ColReorderLevel", (StockItemListRowDto r) => r.ReorderLevel, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColStatus", (StockItemListRowDto r) => EnumText(r.Status))
			};
			return Export("StockItemList.xlsx", "ReportStockItemList", report.Rows, columns, new ExcelTotal[2]
			{
				InventoryExcelExporterBase.Total("TotalItems", report.ItemCount),
				InventoryExcelExporterBase.CostTotal("TotalValue", report.TotalValue)
			});
		}

		public FileDto ExportItemLedger(ItemLedgerReportDto report)
		{
			List<ExcelColumn<ItemLedgerRowDto>> columns = new List<ExcelColumn<ItemLedgerRowDto>>
			{
				InventoryExcelExporterBase.Col("ColDate", (ItemLedgerRowDto r) => InventoryExcelExporterBase.DateOnly(r.MovementDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColWhat", (ItemLedgerRowDto r) => EnumText(r.MovementType)),
				InventoryExcelExporterBase.Col("ColReference", (ItemLedgerRowDto r) => r.ReferenceNo),
				InventoryExcelExporterBase.Col("ColIn", (ItemLedgerRowDto r) => r.QuantityIn, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColOut", (ItemLedgerRowDto r) => r.QuantityOut, "#,##0.####"),
				InventoryExcelExporterBase.CostCol("ColUnitCost", (ItemLedgerRowDto r) => r.UnitCost),
				InventoryExcelExporterBase.CostCol("ColValue", (ItemLedgerRowDto r) => r.Value),
				InventoryExcelExporterBase.Col("ColBalance", (ItemLedgerRowDto r) => r.RunningBalance, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColRemarks", (ItemLedgerRowDto r) => r.Remarks)
			};
			return Export("ItemLedger-" + report.ItemCode + ".xlsx", "ReportItemLedger", report.Rows, columns, new ExcelTotal[6]
			{
				InventoryExcelExporterBase.Total("ColItem", report.ItemCode + " " + report.ItemName + " (" + report.BaseUnitCode + ")"),
				InventoryExcelExporterBase.Total("OpeningBalance", report.OpeningQuantity),
				InventoryExcelExporterBase.Total("TotalIn", report.TotalIn),
				InventoryExcelExporterBase.Total("TotalOut", report.TotalOut),
				InventoryExcelExporterBase.Total("ClosingBalance", report.ClosingQuantity),
				InventoryExcelExporterBase.CostTotal("ClosingValue", report.ClosingValue)
			});
		}

		public FileDto ExportInvoiceList(InvoiceListReportDto report)
		{
			List<ExcelColumn<InvoiceListRowDto>> columns = new List<ExcelColumn<InvoiceListRowDto>>
			{
				InventoryExcelExporterBase.Col("ColNumber", (InvoiceListRowDto r) => r.InvoiceNo),
				InventoryExcelExporterBase.Col("ColDate", (InvoiceListRowDto r) => InventoryExcelExporterBase.DateOnly(r.InvoiceDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColSupplier", (InvoiceListRowDto r) => r.SupplierName),
				InventoryExcelExporterBase.Col("ColSupplierInvoiceNo", (InvoiceListRowDto r) => r.SupplierInvoiceNo),
				InventoryExcelExporterBase.Col("ColLines", (InvoiceListRowDto r) => r.LineCount),
				InventoryExcelExporterBase.Col("ColCurrency", (InvoiceListRowDto r) => r.CurrencyCode),
				InventoryExcelExporterBase.Col("ColExchangeRate", (InvoiceListRowDto r) => r.ExchangeRate, "#,##0.######"),
				InventoryExcelExporterBase.CostCol("ColSubTotal", (InvoiceListRowDto r) => r.SubTotal),
				InventoryExcelExporterBase.CostCol("ColDiscount", (InvoiceListRowDto r) => r.DiscountAmount),
				InventoryExcelExporterBase.CostCol("ColTax", (InvoiceListRowDto r) => r.TaxAmount),
				InventoryExcelExporterBase.CostCol("ColOtherCharges", (InvoiceListRowDto r) => r.OtherCharges),
				InventoryExcelExporterBase.CostCol("ColTotal", (InvoiceListRowDto r) => r.TotalAmount),
				InventoryExcelExporterBase.CostCol("ColTotalBase", (InvoiceListRowDto r) => r.TotalInBaseCurrency),
				InventoryExcelExporterBase.Col("ColStatus", (InvoiceListRowDto r) => EnumText(r.Status))
			};
			return Export("InvoiceList.xlsx", "ReportInvoiceList", report.Rows, columns, new ExcelTotal[4]
			{
				InventoryExcelExporterBase.Total("TotalInvoices", report.InvoiceCount),
				InventoryExcelExporterBase.Total("TotalPostedInvoices", report.PostedCount),
				InventoryExcelExporterBase.CostTotal("TotalPosted", report.PostedTotal),
				InventoryExcelExporterBase.CostTotal("TotalDrafts", report.DraftTotal)
			});
		}

		public FileDto ExportSalesList(SalesListReportDto report)
		{
			List<ExcelColumn<SalesListRowDto>> columns = new List<ExcelColumn<SalesListRowDto>>
			{
				InventoryExcelExporterBase.Col("ColNumber", (SalesListRowDto r) => r.DocumentNo),
				InventoryExcelExporterBase.Col("ColDate", (SalesListRowDto r) => InventoryExcelExporterBase.DateOnly(r.DocumentDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColType", (SalesListRowDto r) => EnumText(r.IssueType)),
				InventoryExcelExporterBase.Col("ColCustomer", (SalesListRowDto r) => r.CustomerName),
				InventoryExcelExporterBase.Col("ColLines", (SalesListRowDto r) => r.LineCount),
				InventoryExcelExporterBase.Col("ColCurrency", (SalesListRowDto r) => r.CurrencyCode),
				InventoryExcelExporterBase.CostCol("ColTotal", (SalesListRowDto r) => r.TotalAmount),
				InventoryExcelExporterBase.CostCol("ColNetSalesBase", (SalesListRowDto r) => r.NetSalesInBaseCurrency),
				InventoryExcelExporterBase.CostCol("ColCost", (SalesListRowDto r) => r.Cost),
				InventoryExcelExporterBase.CostCol("ColMargin", (SalesListRowDto r) => r.Margin),
				InventoryExcelExporterBase.CostCol("ColMarginPercent", (SalesListRowDto r) => r.MarginPercent, "0.00%"),
				InventoryExcelExporterBase.Col("ColStatus", (SalesListRowDto r) => EnumText(r.Status))
			};
			return Export("SalesList.xlsx", "ReportSalesList", report.Rows, columns, new ExcelTotal[6]
			{
				InventoryExcelExporterBase.Total("TotalDocuments", report.DocumentCount),
				InventoryExcelExporterBase.Total("TotalPostedDocuments", report.PostedCount),
				InventoryExcelExporterBase.CostTotal("TotalPostedSales", report.PostedSales),
				InventoryExcelExporterBase.CostTotal("TotalPostedCostOfSales", report.PostedCostOfSales),
				InventoryExcelExporterBase.CostTotal("TotalPostedMargin", report.PostedMargin),
				InventoryExcelExporterBase.CostTotal("TotalPostedIssueCost", report.PostedIssueCost)
			});
		}

		public FileDto ExportInvoiceToSales(InvoiceToSalesReportDto report)
		{
			List<ExcelColumn<InvoiceToSalesRowDto>> columns = new List<ExcelColumn<InvoiceToSalesRowDto>>
			{
				InventoryExcelExporterBase.Col("ColNumber", (InvoiceToSalesRowDto r) => r.InvoiceNo),
				InventoryExcelExporterBase.Col("ColDate", (InvoiceToSalesRowDto r) => InventoryExcelExporterBase.DateOnly(r.InvoiceDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColSupplier", (InvoiceToSalesRowDto r) => r.SupplierName),
				InventoryExcelExporterBase.Col("ColCode", (InvoiceToSalesRowDto r) => r.ItemCode),
				InventoryExcelExporterBase.Col("ColItem", (InvoiceToSalesRowDto r) => r.ItemName),
				InventoryExcelExporterBase.Col("ColUnit", (InvoiceToSalesRowDto r) => r.BaseUnitCode),
				InventoryExcelExporterBase.Col("ColReceived", (InvoiceToSalesRowDto r) => r.ReceivedQuantity, "#,##0.####"),
				InventoryExcelExporterBase.CostCol("ColUnitCost", (InvoiceToSalesRowDto r) => r.UnitCost),
				InventoryExcelExporterBase.Col("ColReturned", (InvoiceToSalesRowDto r) => r.ReturnedQuantity, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColSold", (InvoiceToSalesRowDto r) => r.SoldQuantity, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColIssued", (InvoiceToSalesRowDto r) => r.IssuedQuantity, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColOtherOut", (InvoiceToSalesRowDto r) => r.OtherOutQuantity, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColRemaining", (InvoiceToSalesRowDto r) => r.RemainingQuantity, "#,##0.####"),
				InventoryExcelExporterBase.CostCol("ColCostOfSold", (InvoiceToSalesRowDto r) => r.CostOfSold),
				InventoryExcelExporterBase.CostCol("ColCurrentSalePrice", (InvoiceToSalesRowDto r) => r.CurrentSalePrice),
				InventoryExcelExporterBase.CostCol("ColSalesValue", (InvoiceToSalesRowDto r) => r.SalesValue),
				InventoryExcelExporterBase.CostCol("ColMargin", (InvoiceToSalesRowDto r) => r.Margin),
				InventoryExcelExporterBase.CostCol("ColMarginPercent", (InvoiceToSalesRowDto r) => r.MarginPercent, "0.00%")
			};
			return Export("InvoiceToSales.xlsx", "ReportInvoiceToSales", report.Rows, columns, new ExcelTotal[4]
			{
				InventoryExcelExporterBase.CostTotal("TotalCostOfSold", report.TotalCostOfSold),
				InventoryExcelExporterBase.CostTotal("TotalSalesValue", report.TotalSalesValue),
				InventoryExcelExporterBase.CostTotal("TotalMargin", report.TotalMargin),
				InventoryExcelExporterBase.CostTotal("Note", report.Note)
			});
		}
	}
}
