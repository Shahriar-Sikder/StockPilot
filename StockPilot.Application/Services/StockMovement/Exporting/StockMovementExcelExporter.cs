using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.StockMovement.Dtos;

namespace StockPilot.Services.StockMovement.Exporting
{
	public class StockMovementExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<StockMovementListDto> rows)
		{
			List<ExcelColumn<StockMovementListDto>> columns = new List<ExcelColumn<StockMovementListDto>>
			{
				InventoryExcelExporterBase.Col("ColDate", (StockMovementListDto r) => InventoryExcelExporterBase.DateOnly(r.MovementDate), "yyyy-mm-dd"),
				InventoryExcelExporterBase.Col("ColCode", (StockMovementListDto r) => r.ItemCode),
				InventoryExcelExporterBase.Col("ColItem", (StockMovementListDto r) => r.ItemName),
				InventoryExcelExporterBase.Col("ColWhat", (StockMovementListDto r) => EnumText(r.MovementType)),
				InventoryExcelExporterBase.Col("ColQuantity", (StockMovementListDto r) => r.Quantity, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColUnit", (StockMovementListDto r) => r.BaseUnitCode),
				InventoryExcelExporterBase.CostCol("ColUnitCost", (StockMovementListDto r) => r.UnitCost),
				InventoryExcelExporterBase.CostCol("ColValue", (StockMovementListDto r) => r.TotalValue),
				InventoryExcelExporterBase.Col("ColBalanceAfter", (StockMovementListDto r) => r.BalanceAfter, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColReference", (StockMovementListDto r) => r.ReferenceNo),
				InventoryExcelExporterBase.Col("ColRemarks", (StockMovementListDto r) => r.Remarks),
				InventoryExcelExporterBase.Col("ColPostedTime", (StockMovementListDto r) => r.CreationTime, "yyyy-mm-dd hh:mm")
			};
			return Export("StockHistory.xlsx", "StockHistory", rows, columns);
		}
	}
}
