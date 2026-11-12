using StockPilot.Dto;

namespace StockPilot.Services.Currency.Dtos
{
	public class CurrencyGridInput : PagedSortedAndFilteredInputDto
	{
		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
