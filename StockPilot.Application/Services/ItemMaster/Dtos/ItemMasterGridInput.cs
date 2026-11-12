using StockPilot.Dto;

namespace StockPilot.Services.ItemMaster.Dtos
{
	public class ItemMasterGridInput : PagedSortedAndFilteredInputDto
	{
		public ItemStatusFilter Status { get; set; }

		public int? ItemCategoryId { get; set; }

		public int? ItemSubCategoryId { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
