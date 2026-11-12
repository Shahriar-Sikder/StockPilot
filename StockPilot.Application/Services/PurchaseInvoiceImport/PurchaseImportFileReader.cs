using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace StockPilot.Services.PurchaseInvoiceImport
{
	public static class PurchaseImportFileReader
	{
		public const int MaxBytes = 3145728;

		public const int MaxRows = 5000;

		public const string TooManyRowsMessage = "TooManyRows";

		public static bool IsSupported(string fileName)
		{
			string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
			if (!(extension == ".xlsx"))
			{
				return extension == ".csv";
			}
			return true;
		}

		public static List<PurchaseImportRow> Read(string fileName, byte[] bytes)
		{
			string text = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
			if (!(text == ".xlsx"))
			{
				if (text == ".csv")
				{
					return ReadCsv(bytes);
				}
				return null;
			}
			return ReadXlsx(bytes);
		}

		public static string NormalizeHeader(string header)
		{
			return new string((from c in (header ?? string.Empty).ToLowerInvariant()
				where c >= 'a' && c <= 'z'
				select c).ToArray());
		}

		private static List<PurchaseImportRow> ReadXlsx(byte[] bytes)
		{
			List<PurchaseImportRow> rows = new List<PurchaseImportRow>();
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				using (ExcelPackage package = new ExcelPackage(stream))
				{
					ExcelWorksheet sheet = package.Workbook.Worksheets.FirstOrDefault();
					if (sheet?.Dimension == null)
					{
						return rows;
					}
					int lastColumn = sheet.Dimension.End.Column;
					Dictionary<int, string> headers = new Dictionary<int, string>();
					for (int column = 1; column <= lastColumn; column++)
					{
						string header = NormalizeHeader(sheet.Cells[1, column].Text);
						if (header.Length > 0 && !headers.ContainsValue(header))
						{
							headers[column] = header;
						}
					}
					int rowNumber;
					for (rowNumber = 2; rowNumber <= sheet.Dimension.End.Row; rowNumber++)
					{
						Dictionary<string, object> values = headers.ToDictionary((KeyValuePair<int, string> h) => h.Value, (KeyValuePair<int, string> h) => sheet.Cells[rowNumber, h.Key].Value);
						AddRow(rows, new PurchaseImportRow(rowNumber, values));
					}
					return rows;
				}
			}
		}

		private static List<PurchaseImportRow> ReadCsv(byte[] bytes)
		{
			List<PurchaseImportRow> rows = new List<PurchaseImportRow>();
			string text;
			using (StreamReader reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
			{
				text = reader.ReadToEnd();
			}
			List<List<string>> lines = ParseCsv(text);
			if (lines.Count == 0)
			{
				return rows;
			}
			List<string> headers = lines[0].Select(NormalizeHeader).ToList();
			for (int i = 1; i < lines.Count; i++)
			{
				Dictionary<string, object> values = new Dictionary<string, object>();
				for (int column = 0; column < headers.Count; column++)
				{
					if (headers[column].Length > 0 && !values.ContainsKey(headers[column]))
					{
						values[headers[column]] = ((column < lines[i].Count) ? lines[i][column] : null);
					}
				}
				AddRow(rows, new PurchaseImportRow(i + 1, values));
			}
			return rows;
		}

		private static void AddRow(List<PurchaseImportRow> rows, PurchaseImportRow row)
		{
			if (!row.IsEmpty)
			{
				if (rows.Count >= 5000)
				{
					throw new InvalidDataException("TooManyRows");
				}
				rows.Add(row);
			}
		}

		private static List<List<string>> ParseCsv(string text)
		{
			string firstLine = text.Split('\n').FirstOrDefault() ?? string.Empty;
			char delimiter = ((firstLine.Count((char c2) => c2 == ';') > firstLine.Count((char c2) => c2 == ',')) ? ';' : ',');
			List<List<string>> result = new List<List<string>>();
			List<string> row = new List<string>();
			StringBuilder field = new StringBuilder();
			bool inQuotes = false;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (inQuotes)
				{
					if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
					{
						field.Append('"');
						i++;
					}
					else if (c == '"')
					{
						inQuotes = false;
					}
					else
					{
						field.Append(c);
					}
				}
				else if (c == '"')
				{
					inQuotes = true;
				}
				else if (c == delimiter)
				{
					row.Add(field.ToString());
					field.Clear();
				}
				else if (c == '\n' || c == '\r')
				{
					if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}
					row.Add(field.ToString());
					field.Clear();
					result.Add(row);
					row = new List<string>();
				}
				else
				{
					field.Append(c);
				}
			}
			if (field.Length > 0 || row.Count > 0)
			{
				row.Add(field.ToString());
				result.Add(row);
			}
			return result;
		}

		public static bool TryParseDate(object value, out DateTime date)
		{
			if (value is DateTime)
			{
				date = ((DateTime)value).Date;
				return true;
			}
			if (value is double)
			{
				try
				{
					date = DateTime.FromOADate((double)value).Date;
					return true;
				}
				catch (ArgumentException)
				{
					date = default(DateTime);
					return false;
				}
			}
			string[] formats = new string[6] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MMM-yyyy", "d-MMM-yyyy" };
			if (DateTime.TryParseExact(value?.ToString().Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
			{
				date = date.Date;
				return true;
			}
			return false;
		}

		public static bool TryParseDecimal(object value, out decimal number)
		{
			if (value is double)
			{
				number = (decimal)(double)value;
				return true;
			}
			if (value is decimal || value is int || value is long)
			{
				number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
				return true;
			}
			return decimal.TryParse(value?.ToString().Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out number);
		}
	}
}
