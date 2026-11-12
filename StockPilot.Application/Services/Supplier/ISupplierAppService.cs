using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Supplier.Dtos;

namespace StockPilot.Services.Supplier
{
	public interface ISupplierAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<SupplierListDto>> GetAll(SupplierGridInput input);

		Task<FileDto> GetAllToExcel(SupplierGridInput input);

		Task<EditSupplierDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateSupplierDto input);

		Task Update(EditSupplierDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<SupplierComboDto>> GetForCombo();
	}
}
