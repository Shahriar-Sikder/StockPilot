using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Localization;
using Abp.UI;

namespace StockPilot.Authorization
{
	public class HostLoginPolicy : ITransientDependency
	{
		private readonly ISettingManager _settingManager;

		private readonly ILocalizationManager _localizationManager;

		public HostLoginPolicy(ISettingManager settingManager, ILocalizationManager localizationManager)
		{
			_settingManager = settingManager;
			_localizationManager = localizationManager;
		}

		public virtual async Task CheckAsync(string tenancyName, bool hostLogin)
		{
			if (!string.IsNullOrWhiteSpace(tenancyName) || hostLogin || !(await _settingManager.GetSettingValueForApplicationAsync("App.TenantManagement.HostLoginMode") == "SeparatePage"))
			{
				return;
			}
			throw new UserFriendlyException(_localizationManager.GetSource("StockPilot").GetString("EnterYourStoreName"));
		}
	}
}
