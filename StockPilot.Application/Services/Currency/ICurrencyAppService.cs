using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Currency.Dtos;

namespace StockPilot.Services.Currency
{
	public interface ICurrencyAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<CurrencyListDto>> GetAll(CurrencyGridInput input);

		Task<FileDto> GetAllToExcel(CurrencyGridInput input);

		Task<EditCurrencyDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateCurrencyDto input);

		Task Update(EditCurrencyDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<CurrencyComboDto>> GetForCombo();
	}
}
