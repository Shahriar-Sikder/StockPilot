using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.StockMovement.Dtos;

namespace StockPilot.Services.StockMovement
{
	public interface IStockMovementAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<StockMovementListDto>> GetAll(StockMovementGridInput input);

		Task<FileDto> GetAllToExcel(StockMovementGridInput input);
	}
}
