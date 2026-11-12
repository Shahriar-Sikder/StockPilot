using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Authorization.Users.Dto;

namespace StockPilot.Authorization.Users
{
	public interface IUserLoginAppService : IApplicationService, ITransientDependency
	{
		Task<ListResultDto<UserLoginAttemptDto>> GetRecentUserLoginAttempts();
	}
}
