using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.ItemMaster.Dtos;

namespace StockPilot.Services.ItemMaster.Exporting
{
	public class ItemMasterExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<ItemMasterListDto> rows)
		{
			List<ExcelColumn<ItemMasterListDto>> columns = new List<ExcelColumn<ItemMasterListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (ItemMasterListDto r) => r.ItemCode),
				InventoryExcelExporterBase.Col("ColItem", (ItemMasterListDto r) => r.ItemName),
				InventoryExcelExporterBase.Col("ColBarcode", (ItemMasterListDto r) => r.Barcode),
				InventoryExcelExporterBase.Col("ColCategory", (ItemMasterListDto r) => r.ItemCategoryName),
				InventoryExcelExporterBase.Col("ColSubCategory", (ItemMasterListDto r) => r.ItemSubCategoryName),
				InventoryExcelExporterBase.Col("ColOnHand", (ItemMasterListDto r) => r.QuantityOnHand, "#,##0.####"),
				InventoryExcelExporterBase.Col("ColUnit", (ItemMasterListDto r) => r.BaseUnitCode),
				InventoryExcelExporterBase.CostCol("ColAvgCost", (ItemMasterListDto r) => r.AverageCost),
				InventoryExcelExporterBase.CostCol("ColValue", (ItemMasterListDto r) => r.StockValue),
				InventoryExcelExporterBase.Col("ColReorderLevel", (ItemMasterListDto r) => r.ReorderLevel, "#,##0.####"),
				InventoryExcelExporterBase.CostCol("ColSalePrice", (ItemMasterListDto r) => r.SalePrice),
				InventoryExcelExporterBase.Col("ColStatus", (ItemMasterListDto r) => EnumText(r.Status)),
				InventoryExcelExporterBase.Col("ColActive", (ItemMasterListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Items.xlsx", "Items", rows, columns);
		}
	}
}
