using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using StockPilot.Authorization.Users.Dto;
using StockPilot.MultiTenancy;

namespace StockPilot.Authorization.Users
{
	[AbpAuthorize(new string[] { })]
	public class UserLinkAppService : StockPilotAppServiceBase, IUserLinkAppService, IApplicationService, ITransientDependency
	{
		private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;

		private readonly IUserLinkManager _userLinkManager;

		private readonly IRepository<Tenant> _tenantRepository;

		private readonly IRepository<UserAccount, long> _userAccountRepository;

		private readonly LogInManager _logInManager;

		public UserLinkAppService(AbpLoginResultTypeHelper abpLoginResultTypeHelper, IUserLinkManager userLinkManager, IRepository<Tenant> tenantRepository, IRepository<UserAccount, long> userAccountRepository, LogInManager logInManager)
		{
			_abpLoginResultTypeHelper = abpLoginResultTypeHelper;
			_userLinkManager = userLinkManager;
			_tenantRepository = tenantRepository;
			_userAccountRepository = userAccountRepository;
			_logInManager = logInManager;
		}

		public async Task LinkToUser(LinkToUserInput input)
		{
			AbpLoginResult<Tenant, User> loginResult = await _logInManager.LoginAsync(input.UsernameOrEmailAddress, input.Password, input.TenancyName);
			if (loginResult.Result != AbpLoginResultType.Success)
			{
				throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, input.UsernameOrEmailAddress, input.TenancyName);
			}
			if (base.AbpSession.IsUser(loginResult.User))
			{
				throw new UserFriendlyException(L("YouCannotLinkToSameAccount"));
			}
			if (loginResult.User.ShouldChangePasswordOnNextLogin)
			{
				throw new UserFriendlyException(L("ChangePasswordBeforeLinkToAnAccount"));
			}
			await _userLinkManager.Link(GetCurrentUser(), loginResult.User);
		}

		public async Task<PagedResultDto<LinkedUserDto>> GetLinkedUsers(GetLinkedUsersInput input)
		{
			UserAccount currentUserAccount = await _userLinkManager.GetUserAccountAsync(base.AbpSession.ToUserIdentifier());
			if (currentUserAccount == null)
			{
				return new PagedResultDto<LinkedUserDto>(0, new List<LinkedUserDto>());
			}
			IQueryable<LinkedUserDto> query = CreateLinkedUsersQuery(currentUserAccount, input.Sorting);
			query = query.Skip(input.SkipCount).Take(input.MaxResultCount);
			return new PagedResultDto<LinkedUserDto>(await query.CountAsync(), await query.ToListAsync());
		}

		[DisableAuditing]
		public async Task<ListResultDto<LinkedUserDto>> GetRecentlyUsedLinkedUsers()
		{
			UserAccount currentUserAccount = await _userLinkManager.GetUserAccountAsync(base.AbpSession.ToUserIdentifier());
			if (currentUserAccount == null)
			{
				return new ListResultDto<LinkedUserDto>();
			}
			return new ListResultDto<LinkedUserDto>(await CreateLinkedUsersQuery(currentUserAccount, "LastLoginTime DESC").Skip(0).Take(3).ToListAsync());
		}

		public async Task UnlinkUser(UnlinkUserInput input)
		{
			if (!(await _userLinkManager.GetUserAccountAsync(base.AbpSession.ToUserIdentifier())).UserLinkId.HasValue)
			{
				throw new ApplicationException(L("You are not linked to any account"));
			}
			if (await _userLinkManager.AreUsersLinked(base.AbpSession.ToUserIdentifier(), input.ToUserIdentifier()))
			{
				await _userLinkManager.Unlink(input.ToUserIdentifier());
			}
		}

		private IQueryable<LinkedUserDto> CreateLinkedUsersQuery(UserAccount currentUserAccount, string sorting)
		{
			UserIdentifier currentUserIdentifier = base.AbpSession.ToUserIdentifier();
			return (from userAccount in _userAccountRepository.GetAll()
				join tenant in _tenantRepository.GetAll() on userAccount.TenantId equals tenant.Id into tenantJoined
				from tenant in tenantJoined.DefaultIfEmpty()
				where (userAccount.TenantId != currentUserIdentifier.TenantId || userAccount.UserId != currentUserIdentifier.UserId) && userAccount.UserLinkId.HasValue && userAccount.UserLinkId == currentUserAccount.UserLinkId
				select new LinkedUserDto
				{
					Id = userAccount.UserId,
					TenantId = userAccount.TenantId,
					TenancyName = ((tenant == null) ? "." : tenant.TenancyName),
					Username = userAccount.UserName,
					LastLoginTime = userAccount.LastLoginTime
				}).OrderBy(sorting);
		}
	}
}
