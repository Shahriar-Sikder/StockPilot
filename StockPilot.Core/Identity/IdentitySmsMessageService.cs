using System.Threading.Tasks;
using Abp.Dependency;
using Castle.Core.Logging;
using Microsoft.AspNet.Identity;

namespace StockPilot.Identity
{
	public class IdentitySmsMessageService : IIdentityMessageService, ITransientDependency
	{
		public ILogger Logger { get; set; }

		public IdentitySmsMessageService()
		{
			Logger = NullLogger.Instance;
		}

		public Task SendAsync(IdentityMessage message)
		{
			Logger.Warn("Sending SMS is not implemented! Message content:");
			Logger.Warn("Destination : " + message.Destination);
			Logger.Warn("Subject     : " + message.Subject);
			Logger.Warn("Body        : " + message.Body);
			return Task.FromResult(0);
		}
	}
}
