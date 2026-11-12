using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace StockPilot.Authorization.Users
{
	public class UserLinkManager : StockPilotDomainServiceBase, IUserLinkManager
	{
		private readonly IRepository<UserAccount, long> _userAccountRepository;

		public UserLinkManager(IRepository<UserAccount, long> userAccountRepository)
		{
			_userAccountRepository = userAccountRepository;
		}

		[UnitOfWork]
		public virtual async Task Link(User firstUser, User secondUser)
		{
			UserAccount firstUserAccount = await GetUserAccountAsync(firstUser.ToUserIdentifier());
			UserAccount secondUserAccount = await GetUserAccountAsync(secondUser.ToUserIdentifier());
			long userLinkId = firstUserAccount.UserLinkId ?? firstUserAccount.Id;
			firstUserAccount.UserLinkId = userLinkId;
			(secondUserAccount.UserLinkId.HasValue ? _userAccountRepository.GetAllList((UserAccount ua) => ua.UserLinkId == (long?)secondUserAccount.UserLinkId.Value) : new List<UserAccount> { secondUserAccount }).ForEach(delegate(UserAccount u)
			{
				u.UserLinkId = userLinkId;
			});
			await base.CurrentUnitOfWork.SaveChangesAsync();
		}

		[UnitOfWork]
		public virtual async Task<bool> AreUsersLinked(UserIdentifier firstUserIdentifier, UserIdentifier secondUserIdentifier)
		{
			UserAccount firstUserAccount = await GetUserAccountAsync(firstUserIdentifier);
			UserAccount secondUserAccount = await GetUserAccountAsync(secondUserIdentifier);
			if (!firstUserAccount.UserLinkId.HasValue || !secondUserAccount.UserLinkId.HasValue)
			{
				return false;
			}
			return firstUserAccount.UserLinkId == secondUserAccount.UserLinkId;
		}

		[UnitOfWork]
		public virtual async Task Unlink(UserIdentifier userIdentifier)
		{
			(await GetUserAccountAsync(userIdentifier)).UserLinkId = null;
			await base.CurrentUnitOfWork.SaveChangesAsync();
		}

		[UnitOfWork]
		public virtual async Task<UserAccount> GetUserAccountAsync(UserIdentifier userIdentifier)
		{
			return await _userAccountRepository.FirstOrDefaultAsync((UserAccount ua) => ua.TenantId == userIdentifier.TenantId && ua.UserId == userIdentifier.UserId);
		}
	}
}
