namespace StockPilot.Services.Common.Exporting
{
	public class ExcelTotal
	{
		public string LabelKey { get; }

		public object Value { get; }

		public bool IsCost { get; }

		public ExcelTotal(string labelKey, object value, bool isCost = false)
		{
			LabelKey = labelKey;
			Value = value;
			IsCost = isCost;
		}
	}
}
