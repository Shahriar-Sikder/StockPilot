using StockPilot.Dto;

namespace StockPilot.Services.UnitMaster.Dtos
{
	public class UnitMasterGridInput : PagedSortedAndFilteredInputDto
	{
		public int? UnitSetId { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
