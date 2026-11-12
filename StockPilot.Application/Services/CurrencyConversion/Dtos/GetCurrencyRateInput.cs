using System;
using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.CurrencyConversion.Dtos
{
	public class GetCurrencyRateInput
	{
		[Range(1, int.MaxValue)]
		public int FromCurrencyId { get; set; }

		[Range(1, int.MaxValue)]
		public int ToCurrencyId { get; set; }

		public DateTime Date { get; set; }
	}
}
