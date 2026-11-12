using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Timing.Dto;

namespace StockPilot.Timing
{
	public interface ITimingAppService : IApplicationService, ITransientDependency
	{
		Task<ListResultDto<NameValueDto>> GetTimezones(GetTimezonesInput input);

		Task<List<ComboboxItemDto>> GetTimezoneComboboxItems(GetTimezoneComboboxItemsInput input);
	}
}
