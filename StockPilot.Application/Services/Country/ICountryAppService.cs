using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Country.Dtos;

namespace StockPilot.Services.Country
{
	public interface ICountryAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<CountryListDto>> GetAll(CountryGridInput input);

		Task<FileDto> GetAllToExcel(CountryGridInput input);

		Task<EditCountryDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateCountryDto input);

		Task Update(EditCountryDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<CountryComboDto>> GetForCombo();
	}
}
