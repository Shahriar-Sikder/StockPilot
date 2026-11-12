using System;

namespace StockPilot.Inventory
{
	public class ExchangeRateResult
	{
		public decimal Rate { get; set; }

		public DateTime? EffectiveDate { get; set; }

		public bool IsInverse { get; set; }
	}
}
