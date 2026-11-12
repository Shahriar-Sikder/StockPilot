using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Runtime.Caching;
using StockPilot.Caching.Dto;

namespace StockPilot.Caching
{
	[AbpAuthorize(new string[] { "Pages.Administration.Host.Maintenance" })]
	public class CachingAppService : StockPilotAppServiceBase, ICachingAppService, IApplicationService, ITransientDependency
	{
		private readonly ICacheManager _cacheManager;

		public CachingAppService(ICacheManager cacheManager)
		{
			_cacheManager = cacheManager;
		}

		public ListResultDto<CacheDto> GetAllCaches()
		{
			return new ListResultDto<CacheDto>((from cache in _cacheManager.GetAllCaches()
				select new CacheDto
				{
					Name = cache.Name
				}).ToList());
		}

		public async Task ClearCache(EntityDto<string> input)
		{
			await _cacheManager.GetCache(input.Id).ClearAsync();
		}

		public async Task ClearAllCaches()
		{
			IReadOnlyList<ICache> caches = _cacheManager.GetAllCaches();
			foreach (ICache item in caches)
			{
				await item.ClearAsync();
			}
		}
	}
}
