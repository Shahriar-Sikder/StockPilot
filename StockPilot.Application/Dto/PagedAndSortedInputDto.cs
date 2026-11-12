using Abp.Application.Services.Dto;

namespace StockPilot.Dto
{
	public class PagedAndSortedInputDto : PagedInputDto, ISortedResultRequest
	{
		public string Sorting { get; set; }

		public PagedAndSortedInputDto()
		{
			base.MaxResultCount = 10;
		}
	}
}
