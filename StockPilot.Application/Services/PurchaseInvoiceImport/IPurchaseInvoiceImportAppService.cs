using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.PurchaseInvoiceImport.Dtos;

namespace StockPilot.Services.PurchaseInvoiceImport
{
	public interface IPurchaseInvoiceImportAppService : IApplicationService, ITransientDependency
	{
		FileDto GetTemplate();

		Task<PurchaseImportPreviewDto> Preview(PurchaseImportFileInput input);

		Task<PurchaseImportResultDto> Import(ImportPurchaseInvoicesInput input);
	}
}
