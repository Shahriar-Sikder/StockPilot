using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Dependency;
using StockPilot.Sessions.Dto;

namespace StockPilot.Sessions
{
	[AbpAuthorize(new string[] { })]
	public class SessionAppService : StockPilotAppServiceBase, ISessionAppService, IApplicationService, ITransientDependency
	{
		[DisableAuditing]
		public async Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations()
		{
			GetCurrentLoginInformationsOutput getCurrentLoginInformationsOutput = new GetCurrentLoginInformationsOutput();
			GetCurrentLoginInformationsOutput getCurrentLoginInformationsOutput2 = getCurrentLoginInformationsOutput;
			getCurrentLoginInformationsOutput2.User = (await GetCurrentUserAsync()).MapTo<UserLoginInfoDto>();
			GetCurrentLoginInformationsOutput output = getCurrentLoginInformationsOutput;
			if (base.AbpSession.TenantId.HasValue)
			{
				getCurrentLoginInformationsOutput = output;
				getCurrentLoginInformationsOutput.Tenant = (await GetCurrentTenantAsync()).MapTo<TenantLoginInfoDto>();
			}
			return output;
		}
	}
}
