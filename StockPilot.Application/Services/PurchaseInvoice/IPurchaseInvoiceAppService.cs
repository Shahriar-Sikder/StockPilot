using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.PurchaseInvoice.Dtos;

namespace StockPilot.Services.PurchaseInvoice
{
	public interface IPurchaseInvoiceAppService : IApplicationService, ITransientDependency
	{
		Task<PurchaseInvoicePagedResultDto> GetAll(PurchaseInvoiceGridInput input);

		Task<FileDto> GetAllToExcel(PurchaseInvoiceGridInput input);

		Task<EditPurchaseInvoiceDto> Get(EntityDto input);

		Task<EntityDto> Create(CreatePurchaseInvoiceDto input);

		Task Update(EditPurchaseInvoiceDto input);

		Task DeleteDraft(EntityDto input);

		Task Post(EntityDto input);

		Task Cancel(EntityDto input);

		Task<PurchaseInvoiceDocumentDto> UploadDocument(UploadPurchaseDocumentInput input);

		Task<PurchaseDocumentContentDto> DownloadDocument(EntityDto input);

		Task DeleteDocument(EntityDto input);

		Task RestoreDocument(EntityDto input);
	}
}
