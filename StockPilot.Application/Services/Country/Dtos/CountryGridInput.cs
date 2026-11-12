using StockPilot.Dto;

namespace StockPilot.Services.Country.Dtos
{
	public class CountryGridInput : PagedSortedAndFilteredInputDto
	{
		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
