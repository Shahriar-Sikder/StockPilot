using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace StockPilot.Dto
{
	public class PagedAndFilteredInputDto : IPagedResultRequest, ILimitedResultRequest
	{
		[Range(1, 1000)]
		public int MaxResultCount { get; set; }

		[Range(0, int.MaxValue)]
		public int SkipCount { get; set; }

		public string Filter { get; set; }

		public PagedAndFilteredInputDto()
		{
			MaxResultCount = 10;
		}
	}
}
