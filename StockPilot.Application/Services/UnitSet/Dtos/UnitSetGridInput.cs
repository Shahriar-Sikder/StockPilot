using StockPilot.Dto;

namespace StockPilot.Services.UnitSet.Dtos
{
	public class UnitSetGridInput : PagedSortedAndFilteredInputDto
	{
		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
