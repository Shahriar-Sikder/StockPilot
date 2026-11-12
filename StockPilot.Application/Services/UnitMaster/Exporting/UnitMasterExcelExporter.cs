using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.UnitMaster.Dtos;

namespace StockPilot.Services.UnitMaster.Exporting
{
	public class UnitMasterExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<UnitMasterListDto> rows)
		{
			List<ExcelColumn<UnitMasterListDto>> columns = new List<ExcelColumn<UnitMasterListDto>>
			{
				InventoryExcelExporterBase.Col("ColUnitGroup", (UnitMasterListDto r) => r.UnitSetName),
				InventoryExcelExporterBase.Col("ColCode", (UnitMasterListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (UnitMasterListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColBaseUnit", (UnitMasterListDto r) => (!r.IsBaseUnit) ? L("No") : L("Yes")),
				InventoryExcelExporterBase.Col("ColActive", (UnitMasterListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Units.xlsx", "Units", rows, columns);
		}
	}
}
