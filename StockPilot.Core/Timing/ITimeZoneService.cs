using System.Threading.Tasks;
using Abp.Configuration;

namespace StockPilot.Timing
{
	public interface ITimeZoneService
	{
		Task<string> GetDefaultTimezoneAsync(SettingScopes scope, int? tenantId);
	}
}
