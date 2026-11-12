using System;

namespace StockPilot.Services.Common.Exporting
{
	public class ExcelColumn<T>
	{
		public string HeaderKey { get; }

		public Func<T, object> Value { get; }

		public string NumberFormat { get; }

		public bool IsCost { get; }

		public ExcelColumn(string headerKey, Func<T, object> value, string numberFormat = null, bool isCost = false)
		{
			HeaderKey = headerKey;
			Value = value;
			NumberFormat = numberFormat;
			IsCost = isCost;
		}
	}
}
