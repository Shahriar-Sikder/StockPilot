using System.Threading.Tasks;

namespace StockPilot.Authorization.Users
{
	public interface IUserEmailer
	{
		Task SendEmailActivationLinkAsync(User user, string plainPassword = null);

		Task SendPasswordResetLinkAsync(User user);
	}
}
