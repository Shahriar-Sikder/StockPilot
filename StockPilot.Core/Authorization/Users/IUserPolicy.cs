using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Policies;

namespace StockPilot.Authorization.Users
{
	public interface IUserPolicy : IPolicy, ITransientDependency
	{
		Task CheckMaxUserCountAsync(int tenantId);
	}
}
