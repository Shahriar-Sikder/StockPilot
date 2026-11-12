using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.UnitConversion.Dtos;

namespace StockPilot.Services.UnitConversion.Exporting
{
	public class UnitConversionExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<UnitConversionListDto> rows)
		{
			List<ExcelColumn<UnitConversionListDto>> columns = new List<ExcelColumn<UnitConversionListDto>>
			{
				InventoryExcelExporterBase.Col("ColUnitGroup", (UnitConversionListDto r) => r.UnitSetName),
				InventoryExcelExporterBase.Col("ColOneOf", (UnitConversionListDto r) => r.FromUnitCode),
				InventoryExcelExporterBase.Col("ColEqualsIn", (UnitConversionListDto r) => r.ToUnitCode),
				InventoryExcelExporterBase.Col("ColFactor", (UnitConversionListDto r) => r.Factor, "#,##0.######"),
				InventoryExcelExporterBase.Col("ColActive", (UnitConversionListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("UnitConversions.xlsx", "Unit conversions", rows, columns);
		}
	}
}
