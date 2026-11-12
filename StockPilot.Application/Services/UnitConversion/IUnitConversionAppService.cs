using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.UnitConversion.Dtos;

namespace StockPilot.Services.UnitConversion
{
	public interface IUnitConversionAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<UnitConversionListDto>> GetAll(UnitConversionGridInput input);

		Task<FileDto> GetAllToExcel(UnitConversionGridInput input);

		Task<EditUnitConversionDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateUnitConversionDto input);

		Task Update(EditUnitConversionDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<UnitConversionComboDto>> GetForCombo();
	}
}
