using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.CurrencyConversion.Dtos;

namespace StockPilot.Services.CurrencyConversion
{
	public interface ICurrencyConversionAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<CurrencyConversionListDto>> GetAll(CurrencyConversionGridInput input);

		Task<FileDto> GetAllToExcel(CurrencyConversionGridInput input);

		Task<EditCurrencyConversionDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateCurrencyConversionDto input);

		Task Update(EditCurrencyConversionDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<CurrencyConversionComboDto>> GetForCombo();

		Task<CurrencyRateDto> GetRate(GetCurrencyRateInput input);
	}
}
