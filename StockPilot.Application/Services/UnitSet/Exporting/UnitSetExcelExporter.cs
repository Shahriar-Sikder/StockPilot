using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.UnitSet.Dtos;

namespace StockPilot.Services.UnitSet.Exporting
{
	public class UnitSetExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<UnitSetListDto> rows)
		{
			List<ExcelColumn<UnitSetListDto>> columns = new List<ExcelColumn<UnitSetListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (UnitSetListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (UnitSetListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColDescription", (UnitSetListDto r) => r.Description),
				InventoryExcelExporterBase.Col("ColActive", (UnitSetListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("UnitGroups.xlsx", "Unit groups", rows, columns);
		}
	}
}
