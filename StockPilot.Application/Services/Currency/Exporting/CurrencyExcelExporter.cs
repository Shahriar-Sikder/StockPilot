using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.Currency.Dtos;

namespace StockPilot.Services.Currency.Exporting
{
	public class CurrencyExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<CurrencyListDto> rows)
		{
			List<ExcelColumn<CurrencyListDto>> columns = new List<ExcelColumn<CurrencyListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (CurrencyListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (CurrencyListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColSymbol", (CurrencyListDto r) => r.Symbol),
				InventoryExcelExporterBase.Col("ColDecimals", (CurrencyListDto r) => r.DecimalPlaces),
				InventoryExcelExporterBase.Col("ColBaseCurrency", (CurrencyListDto r) => (!r.IsBaseCurrency) ? L("No") : L("Yes")),
				InventoryExcelExporterBase.Col("ColActive", (CurrencyListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Currencies.xlsx", "Currencies", rows, columns);
		}
	}
}
