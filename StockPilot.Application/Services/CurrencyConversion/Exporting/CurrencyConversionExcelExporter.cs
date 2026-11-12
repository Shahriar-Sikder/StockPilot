using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.CurrencyConversion.Dtos;

namespace StockPilot.Services.CurrencyConversion.Exporting
{
	public class CurrencyConversionExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<CurrencyConversionListDto> rows)
		{
			List<ExcelColumn<CurrencyConversionListDto>> columns = new List<ExcelColumn<CurrencyConversionListDto>>
			{
				InventoryExcelExporterBase.Col("ColFrom", (CurrencyConversionListDto r) => r.FromCurrencyCode),
				InventoryExcelExporterBase.Col("ColTo", (CurrencyConversionListDto r) => r.ToCurrencyCode),
				InventoryExcelExporterBase.Col("ColRate", (CurrencyConversionListDto r) => r.Rate, "#,##0.######"),
				InventoryExcelExporterBase.Col("ColEffectiveDate", (CurrencyConversionListDto r) => InventoryExcelExporterBase.DateOnly(r.EffectiveDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColActive", (CurrencyConversionListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("ExchangeRates.xlsx", "Exchange rates", rows, columns);
		}
	}
}
