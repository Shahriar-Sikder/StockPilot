using StockPilot.Dto;

namespace StockPilot.Services.ItemSubCategory.Dtos
{
	public class ItemSubCategoryGridInput : PagedSortedAndFilteredInputDto
	{
		public int? ItemCategoryId { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
