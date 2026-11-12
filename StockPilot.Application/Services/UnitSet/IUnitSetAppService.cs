using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.UnitSet.Dtos;

namespace StockPilot.Services.UnitSet
{
	public interface IUnitSetAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<UnitSetListDto>> GetAll(UnitSetGridInput input);

		Task<FileDto> GetAllToExcel(UnitSetGridInput input);

		Task<EditUnitSetDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateUnitSetDto input);

		Task Update(EditUnitSetDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<UnitSetComboDto>> GetForCombo();
	}
}
