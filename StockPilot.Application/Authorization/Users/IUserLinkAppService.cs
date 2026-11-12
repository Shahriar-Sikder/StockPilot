using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Authorization.Users.Dto;

namespace StockPilot.Authorization.Users
{
	public interface IUserLinkAppService : IApplicationService, ITransientDependency
	{
		Task LinkToUser(LinkToUserInput linkToUserInput);

		Task<PagedResultDto<LinkedUserDto>> GetLinkedUsers(GetLinkedUsersInput input);

		Task<ListResultDto<LinkedUserDto>> GetRecentlyUsedLinkedUsers();

		Task UnlinkUser(UnlinkUserInput input);
	}
}
