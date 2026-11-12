using StockPilot.Dto;

namespace StockPilot.Services.ItemCategory.Dtos
{
	public class ItemCategoryGridInput : PagedSortedAndFilteredInputDto
	{
		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
