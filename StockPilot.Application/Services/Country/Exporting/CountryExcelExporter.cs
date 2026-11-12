using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.Country.Dtos;

namespace StockPilot.Services.Country.Exporting
{
	public class CountryExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<CountryListDto> rows)
		{
			List<ExcelColumn<CountryListDto>> columns = new List<ExcelColumn<CountryListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (CountryListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (CountryListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColPhoneCode", (CountryListDto r) => r.PhoneCode),
				InventoryExcelExporterBase.Col("ColActive", (CountryListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Countries.xlsx", "Countries", rows, columns);
		}
	}
}
