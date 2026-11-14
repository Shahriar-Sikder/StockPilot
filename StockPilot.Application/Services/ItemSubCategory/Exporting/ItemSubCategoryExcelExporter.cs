using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.ItemSubCategory.Dtos;

namespace StockPilot.Services.ItemSubCategory.Exporting
{
	public class ItemSubCategoryExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<ItemSubCategoryListDto> rows)
		{
			List<ExcelColumn<ItemSubCategoryListDto>> columns = new List<ExcelColumn<ItemSubCategoryListDto>>
			{
				InventoryExcelExporterBase.Col("ColCategory", (ItemSubCategoryListDto r) => r.ItemCategoryName),
				InventoryExcelExporterBase.Col("ColCode", (ItemSubCategoryListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (ItemSubCategoryListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColDescription", (ItemSubCategoryListDto r) => r.Description),
				InventoryExcelExporterBase.Col("ColPicture", (ItemSubCategoryListDto r) => string.IsNullOrEmpty(r.PictureUrl) ? L("No") : L("Yes")),
				InventoryExcelExporterBase.Col("ColActive", (ItemSubCategoryListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("SubCategories.xlsx", "Sub-categories", rows, columns);
		}
	}
}
