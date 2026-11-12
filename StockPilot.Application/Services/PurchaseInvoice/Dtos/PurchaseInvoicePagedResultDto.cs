using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	public class PurchaseInvoicePagedResultDto : PagedResultDto<PurchaseInvoiceListDto>
	{
		public int AllCount { get; set; }

		public int DraftCount { get; set; }

		public int PostedCount { get; set; }

		public int CancelledCount { get; set; }

		public PurchaseInvoicePagedResultDto()
		{
		}

		public PurchaseInvoicePagedResultDto(int totalCount, IReadOnlyList<PurchaseInvoiceListDto> items)
			: base(totalCount, items)
		{
		}
	}
}
