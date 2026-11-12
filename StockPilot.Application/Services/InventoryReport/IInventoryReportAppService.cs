using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.InventoryReport.Dtos;

namespace StockPilot.Services.InventoryReport
{
	public interface IInventoryReportAppService : IApplicationService, ITransientDependency
	{
		Task<StockItemListReportDto> GetStockItemList(StockItemListInput input);

		Task<ItemLedgerReportDto> GetItemLedger(ItemLedgerInput input);

		Task<InvoiceListReportDto> GetInvoiceList(InvoiceListInput input);

		Task<SalesListReportDto> GetSalesList(SalesListInput input);

		Task<InvoiceToSalesReportDto> GetInvoiceToSales(InvoiceToSalesInput input);

		Task<FileDto> GetStockItemListToExcel(StockItemListInput input);

		Task<FileDto> GetItemLedgerToExcel(ItemLedgerInput input);

		Task<FileDto> GetInvoiceListToExcel(InvoiceListInput input);

		Task<FileDto> GetSalesListToExcel(SalesListInput input);

		Task<FileDto> GetInvoiceToSalesToExcel(InvoiceToSalesInput input);
	}
}
