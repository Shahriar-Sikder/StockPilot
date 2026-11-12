using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.ItemMaster.Dtos
{
	public class ItemMasterPagedResultDto : PagedResultDto<ItemMasterListDto>
	{
		public int AllCount { get; set; }

		public int InStockCount { get; set; }

		public int NeedsReorderCount { get; set; }

		public ItemMasterPagedResultDto()
		{
		}

		public ItemMasterPagedResultDto(int totalCount, IReadOnlyList<ItemMasterListDto> items)
			: base(totalCount, items)
		{
		}
	}
}
