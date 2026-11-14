using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.ItemCategory.Dtos;

namespace StockPilot.Services.ItemCategory.Exporting
{
	public class ItemCategoryExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<ItemCategoryListDto> rows)
		{
			List<ExcelColumn<ItemCategoryListDto>> columns = new List<ExcelColumn<ItemCategoryListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (ItemCategoryListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (ItemCategoryListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColDescription", (ItemCategoryListDto r) => r.Description),
				InventoryExcelExporterBase.Col("ColPicture", (ItemCategoryListDto r) => string.IsNullOrEmpty(r.PictureUrl) ? L("No") : L("Yes")),
				InventoryExcelExporterBase.Col("ColActive", (ItemCategoryListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Categories.xlsx", "Categories", rows, columns);
		}
	}
}
