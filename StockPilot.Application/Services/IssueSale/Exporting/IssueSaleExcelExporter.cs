using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.IssueSale.Dtos;

namespace StockPilot.Services.IssueSale.Exporting
{
	public class IssueSaleExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<IssueSaleListDto> rows)
		{
			List<ExcelColumn<IssueSaleListDto>> columns = new List<ExcelColumn<IssueSaleListDto>>
			{
				InventoryExcelExporterBase.Col("ColNumber", (IssueSaleListDto r) => r.DocumentNo),
				InventoryExcelExporterBase.Col("ColDate", (IssueSaleListDto r) => InventoryExcelExporterBase.DateOnly(r.DocumentDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColType", (IssueSaleListDto r) => EnumText(r.IssueType)),
				InventoryExcelExporterBase.Col("ColCustomer", (IssueSaleListDto r) => r.CustomerName),
				InventoryExcelExporterBase.Col("ColReference", (IssueSaleListDto r) => r.ReferenceNo),
				InventoryExcelExporterBase.Col("ColCurrency", (IssueSaleListDto r) => r.CurrencyCode),
				InventoryExcelExporterBase.Col("ColLines", (IssueSaleListDto r) => r.LineCount),
				InventoryExcelExporterBase.CostCol("ColSubTotal", (IssueSaleListDto r) => r.SubTotal),
				InventoryExcelExporterBase.CostCol("ColDiscount", (IssueSaleListDto r) => r.DiscountAmount),
				InventoryExcelExporterBase.CostCol("ColTax", (IssueSaleListDto r) => r.TaxAmount),
				InventoryExcelExporterBase.CostCol("ColTotal", (IssueSaleListDto r) => r.TotalAmount),
				InventoryExcelExporterBase.CostCol("ColCost", (IssueSaleListDto r) => r.CostTotal),
				InventoryExcelExporterBase.Col("ColStatus", (IssueSaleListDto r) => EnumText(r.Status)),
				InventoryExcelExporterBase.Col("ColPostedTime", (IssueSaleListDto r) => r.PostedTime, "yyyy-mm-dd hh:mm")
			};
			return Export("SalesAndIssues.xlsx", "SalesAndIssues", rows, columns);
		}
	}
}
