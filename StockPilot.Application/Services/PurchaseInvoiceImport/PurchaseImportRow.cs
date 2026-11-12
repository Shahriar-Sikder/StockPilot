using System.Collections.Generic;

namespace StockPilot.Services.PurchaseInvoiceImport
{
	public class PurchaseImportRow
	{
		public int RowNumber { get; }

		public Dictionary<string, object> Values { get; }

		public bool IsEmpty
		{
			get
			{
				foreach (object value in Values.Values)
				{
					if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
					{
						return false;
					}
				}
				return true;
			}
		}

		public PurchaseImportRow(int rowNumber, Dictionary<string, object> values)
		{
			RowNumber = rowNumber;
			Values = values;
		}

		public object Get(params string[] aliases)
		{
			foreach (string alias in aliases)
			{
				if (Values.TryGetValue(alias, out var value) && value != null && (!(value is string) || !string.IsNullOrWhiteSpace((string)value)))
				{
					return value;
				}
			}
			return null;
		}

		public string GetText(params string[] aliases)
		{
			return Get(aliases)?.ToString().Trim() ?? string.Empty;
		}
	}
}
