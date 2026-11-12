using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Caching.Dto;

namespace StockPilot.Caching
{
	public interface ICachingAppService : IApplicationService, ITransientDependency
	{
		ListResultDto<CacheDto> GetAllCaches();

		Task ClearCache(EntityDto<string> input);

		Task ClearAllCaches();
	}
}
