using System;
using StockPilot.Dto;

namespace StockPilot.Services.CurrencyConversion.Dtos
{
	public class CurrencyConversionGridInput : PagedSortedAndFilteredInputDto
	{
		public int? FromCurrencyId { get; set; }

		public int? ToCurrencyId { get; set; }

		public DateTime? EffectiveFrom { get; set; }

		public DateTime? EffectiveTo { get; set; }

		public bool? IsActive { get; set; }

		public bool IncludeDeleted { get; set; }
	}
}
