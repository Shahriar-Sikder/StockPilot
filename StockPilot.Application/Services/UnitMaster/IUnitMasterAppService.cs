using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.UnitMaster.Dtos;

namespace StockPilot.Services.UnitMaster
{
	public interface IUnitMasterAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<UnitMasterListDto>> GetAll(UnitMasterGridInput input);

		Task<FileDto> GetAllToExcel(UnitMasterGridInput input);

		Task<EditUnitMasterDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateUnitMasterDto input);

		Task Update(EditUnitMasterDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<UnitMasterComboDto>> GetForCombo();
	}
}
