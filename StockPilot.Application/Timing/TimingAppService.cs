using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Configuration;
using Abp.Dependency;
using StockPilot.Timing.Dto;

namespace StockPilot.Timing
{
	public class TimingAppService : StockPilotAppServiceBase, ITimingAppService, IApplicationService, ITransientDependency
	{
		private readonly ITimeZoneService _timeZoneService;

		public TimingAppService(ITimeZoneService timeZoneService)
		{
			_timeZoneService = timeZoneService;
		}

		public async Task<ListResultDto<NameValueDto>> GetTimezones(GetTimezonesInput input)
		{
			return new ListResultDto<NameValueDto>(await GetTimezoneInfos(input.DefaultTimezoneScope));
		}

		public async Task<List<ComboboxItemDto>> GetTimezoneComboboxItems(GetTimezoneComboboxItemsInput input)
		{
			List<ComboboxItemDto> timeZoneItems = new ListResultDto<ComboboxItemDto>((await GetTimezoneInfos(input.DefaultTimezoneScope)).Select((NameValueDto e) => new ComboboxItemDto(e.Value, e.Name)).ToList()).Items.ToList();
			if (!string.IsNullOrEmpty(input.SelectedTimezoneId))
			{
				ComboboxItemDto selectedEdition = timeZoneItems.FirstOrDefault((ComboboxItemDto e) => e.Value == input.SelectedTimezoneId);
				if (selectedEdition != null)
				{
					selectedEdition.IsSelected = true;
				}
			}
			return timeZoneItems;
		}

		private async Task<List<NameValueDto>> GetTimezoneInfos(SettingScopes defaultTimezoneScope)
		{
			TimeZoneInfo defaultTimezone = TimeZoneInfo.FindSystemTimeZoneById(await _timeZoneService.GetDefaultTimezoneAsync(defaultTimezoneScope, base.AbpSession.TenantId));
			string defaultTimezoneName = string.Format("{0} [{1}]", L("Default"), defaultTimezone.DisplayName);
			List<NameValueDto> list = (from tz in TimeZoneInfo.GetSystemTimeZones()
				select new NameValueDto(tz.DisplayName, tz.Id)).ToList();
			list.Insert(0, new NameValueDto(defaultTimezoneName, string.Empty));
			return list;
		}
	}
}
