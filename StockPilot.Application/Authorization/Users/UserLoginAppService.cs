using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using StockPilot.Authorization.Users.Dto;

namespace StockPilot.Authorization.Users
{
	[AbpAuthorize(new string[] { })]
	public class UserLoginAppService : StockPilotAppServiceBase, IUserLoginAppService, IApplicationService, ITransientDependency
	{
		private readonly IRepository<UserLoginAttempt, long> _userLoginAttemptRepository;

		public UserLoginAppService(IRepository<UserLoginAttempt, long> userLoginAttemptRepository)
		{
			_userLoginAttemptRepository = userLoginAttemptRepository;
		}

		[DisableAuditing]
		public async Task<ListResultDto<UserLoginAttemptDto>> GetRecentUserLoginAttempts()
		{
			long userId = base.AbpSession.GetUserId();
			return new ListResultDto<UserLoginAttemptDto>((await (from la in _userLoginAttemptRepository.GetAll()
				where la.UserId == (long?)userId
				orderby la.CreationTime descending
				select la).Take(20).ToListAsync()).MapTo<List<UserLoginAttemptDto>>());
		}
	}
}
