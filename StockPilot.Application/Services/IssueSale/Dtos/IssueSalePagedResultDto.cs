using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.IssueSale.Dtos
{
	public class IssueSalePagedResultDto : PagedResultDto<IssueSaleListDto>
	{
		public int AllCount { get; set; }

		public int DraftCount { get; set; }

		public int PostedCount { get; set; }

		public int CancelledCount { get; set; }

		public IssueSalePagedResultDto()
		{
		}

		public IssueSalePagedResultDto(int totalCount, IReadOnlyList<IssueSaleListDto> items)
			: base(totalCount, items)
		{
		}
	}
}
