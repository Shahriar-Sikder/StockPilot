using System;
using System.Linq;
using Abp.Timing;
using OfficeOpenXml;
using StockPilot.DataExporting.Excel.EpPlus;
using StockPilot.Dto;

namespace StockPilot.Services.PurchaseInvoiceImport.Exporting
{
	public class PurchaseImportTemplateExporter : EpPlusExcelExporterBase
	{
		public static readonly string[] Columns = new string[8] { "InvoiceNo", "InvoiceDate", "SupplierCode", "ItemCode", "Unit", "Quantity", "UnitCost", "Currency" };

		public FileDto CreateTemplate()
		{
			return CreateExcelPackage("receive-stock-template.xlsx", delegate(ExcelPackage package)
			{
				ExcelWorksheet excelWorksheet = package.Workbook.Worksheets.Add("Receive stock");
				AddHeader(excelWorksheet, Columns);
				DateTime date = Clock.Now.Date;
				object[][] source = new object[3][]
				{
					new object[8] { "INV-1001", date, "SUP-001", "ITM-003", "BOX", 5, 1440, "BDT" },
					new object[8] { "INV-1001", date, "SUP-001", "ITM-004", "PCS", 10, 1500, "BDT" },
					new object[8] { "INV-1002", date, "SUP-002", "ITM-005", "KG", 20, 600, "BDT" }
				};
				AddObjects(excelWorksheet, 2, source.ToList(), Enumerable.Range(0, Columns.Length).Select((Func<int, Func<object[], object>>)((int i) => (object[] row) => row[i])).ToArray());
				excelWorksheet.Column(2).Style.Numberformat.Format = "yyyy-mm-dd";
				for (int num = 1; num <= Columns.Length; num++)
				{
					excelWorksheet.Column(num).AutoFit();
				}
			});
		}
	}
}
