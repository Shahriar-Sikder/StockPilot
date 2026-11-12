using StockPilot.Dto;

namespace StockPilot.Services.UnitConversion.Dtos
{
	public class UnitConversionGridInput : PagedSortedAndFilteredInputDto
	{
		public int? UnitSetId { get; set; }

		public int? FromUnitId { get; set; }

		public int? ToUnitId { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
