using System;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;

namespace StockPilot.Timing
{
	public class TimeZoneService : ITimeZoneService, ITransientDependency
	{
		private readonly ISettingManager _settingManager;

		private readonly ISettingDefinitionManager _settingDefinitionManager;

		public TimeZoneService(ISettingManager settingManager, ISettingDefinitionManager settingDefinitionManager)
		{
			_settingManager = settingManager;
			_settingDefinitionManager = settingDefinitionManager;
		}

		public async Task<string> GetDefaultTimezoneAsync(SettingScopes scope, int? tenantId)
		{
			switch (scope)
			{
			case SettingScopes.User:
				if (tenantId.HasValue)
				{
					return await _settingManager.GetSettingValueForTenantAsync("Abp.Timing.TimeZone", tenantId.Value);
				}
				return await _settingManager.GetSettingValueForApplicationAsync("Abp.Timing.TimeZone");
			case SettingScopes.Tenant:
				return await _settingManager.GetSettingValueForApplicationAsync("Abp.Timing.TimeZone");
			case SettingScopes.Application:
				return _settingDefinitionManager.GetSettingDefinition("Abp.Timing.TimeZone").DefaultValue;
			default:
				throw new Exception("Unknown scope for default timezone setting.");
			}
		}
	}
}
