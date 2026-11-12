using System;

namespace StockPilot.Services.CurrencyConversion.Dtos
{
	public class CurrencyRateDto
	{
		public decimal Rate { get; set; }

		public DateTime? EffectiveDate { get; set; }

		public bool IsInverse { get; set; }
	}
}
