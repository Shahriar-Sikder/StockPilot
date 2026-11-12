using StockPilot.Dto;

namespace StockPilot.Services.Supplier.Dtos
{
	public class SupplierGridInput : PagedSortedAndFilteredInputDto
	{
		public int? CountryId { get; set; }

		public int? CurrencyId { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
