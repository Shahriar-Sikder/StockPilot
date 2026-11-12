using System.Collections.Generic;
using StockPilot.Dto;
using StockPilot.Services.Common.Exporting;
using StockPilot.Services.Supplier.Dtos;

namespace StockPilot.Services.Supplier.Exporting
{
	public class SupplierExcelExporter : InventoryExcelExporterBase
	{
		public FileDto Export(List<SupplierListDto> rows)
		{
			List<ExcelColumn<SupplierListDto>> columns = new List<ExcelColumn<SupplierListDto>>
			{
				InventoryExcelExporterBase.Col("ColCode", (SupplierListDto r) => r.Code),
				InventoryExcelExporterBase.Col("ColName", (SupplierListDto r) => r.Name),
				InventoryExcelExporterBase.Col("ColContact", (SupplierListDto r) => r.ContactPerson),
				InventoryExcelExporterBase.Col("ColPhone", (SupplierListDto r) => r.Phone),
				InventoryExcelExporterBase.Col("ColEmail", (SupplierListDto r) => r.Email),
				InventoryExcelExporterBase.Col("ColCity", (SupplierListDto r) => r.City),
				InventoryExcelExporterBase.Col("ColCountry", (SupplierListDto r) => r.CountryName),
				InventoryExcelExporterBase.Col("ColCurrency", (SupplierListDto r) => r.CurrencyCode),
				InventoryExcelExporterBase.Col("ColPaymentDays", (SupplierListDto r) => r.PaymentTermDays),
				InventoryExcelExporterBase.Col("ColActive", (SupplierListDto r) => (!r.IsActive) ? L("No") : L("Yes"))
			};
			return Export("Suppliers.xlsx", "Suppliers", rows, columns);
		}
	}
}
