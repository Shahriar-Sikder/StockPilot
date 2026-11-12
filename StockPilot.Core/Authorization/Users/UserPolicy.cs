using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Dependency;
using Abp.Domain.Policies;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.UI;

namespace StockPilot.Authorization.Users
{
	public class UserPolicy : StockPilotServiceBase, IUserPolicy, IPolicy, ITransientDependency
	{
		private readonly IFeatureChecker _featureChecker;

		private readonly IRepository<User, long> _userRepository;

		public UserPolicy(IFeatureChecker featureChecker, IRepository<User, long> userRepository)
		{
			_featureChecker = featureChecker;
			_userRepository = userRepository;
		}

		public async Task CheckMaxUserCountAsync(int tenantId)
		{
			int maxUserCount = (await _featureChecker.GetValueAsync(tenantId, "App.MaxUserCount")).To<int>();
			if (maxUserCount <= 0 || await _userRepository.CountAsync() < maxUserCount)
			{
				return;
			}
			throw new UserFriendlyException(L("MaximumUserCount_Error_Message"), L("MaximumUserCount_Error_Detail", maxUserCount));
		}
	}
}
