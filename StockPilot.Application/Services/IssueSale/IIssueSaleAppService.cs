using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.IssueSale.Dtos;

namespace StockPilot.Services.IssueSale
{
	public interface IIssueSaleAppService : IApplicationService, ITransientDependency
	{
		Task<IssueSalePagedResultDto> GetAll(IssueSaleGridInput input);

		Task<FileDto> GetAllToExcel(IssueSaleGridInput input);

		Task<EditIssueSaleDto> Get(EntityDto input);

		Task<EntityDto> Create(CreateIssueSaleDto input);

		Task Update(EditIssueSaleDto input);

		Task DeleteDraft(EntityDto input);

		Task Post(EntityDto input);

		Task Cancel(EntityDto input);

		Task<ListResultDto<string>> GetCustomers(GetCustomersInput input);
	}
}
