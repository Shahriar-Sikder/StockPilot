using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Application.Features;
using Abp.Runtime.Session;
using OfficeOpenXml;
using StockPilot.DataExporting.Excel.EpPlus;
using StockPilot.Dto;

namespace StockPilot.Services.Common.Exporting
{
	public abstract class InventoryExcelExporterBase : EpPlusExcelExporterBase
	{
		public const string QuantityFormat = "#,##0.####";

		public const string MoneyFormat = "#,##0.00";

		public const string RateFormat = "#,##0.######";

		public const string PercentFormat = "0.00%";

		public const string DateFormat = "yyyy-mm-dd";

		public const string DateTimeFormat = "yyyy-mm-dd hh:mm";

		public IFeatureChecker FeatureChecker { get; set; }

		public IAbpSession AbpSession { get; set; } = NullAbpSession.Instance;

		protected FileDto Export<T>(string fileName, string sheetNameKey, IList<T> rows, IList<ExcelColumn<T>> columns, IList<ExcelTotal> totals = null)
		{
			if (IsCostHidden())
			{
				columns = columns.Where((ExcelColumn<T> c) => !c.IsCost).ToList();
				totals = totals?.Where((ExcelTotal t) => !t.IsCost).ToList();
			}
			return CreateExcelPackage(fileName, delegate(ExcelPackage package)
			{
				ExcelWorksheet excelWorksheet = package.Workbook.Worksheets.Add(Truncate(L(sheetNameKey), 31));
				AddHeader(excelWorksheet, columns.Select((ExcelColumn<T> c) => L(c.HeaderKey)).ToArray());
				AddObjects(excelWorksheet, 2, rows, columns.Select((ExcelColumn<T> c) => c.Value).ToArray());
				int toRow = Math.Max(2, rows.Count + 1);
				for (int num = 0; num < columns.Count; num++)
				{
					if (columns[num].NumberFormat != null)
					{
						excelWorksheet.Cells[2, num + 1, toRow, num + 1].Style.Numberformat.Format = columns[num].NumberFormat;
					}
				}
				excelWorksheet.View.FreezePanes(2, 1);
				if (columns.Count > 0)
				{
					excelWorksheet.Cells[1, 1, toRow, columns.Count].AutoFilter = true;
				}
				if (totals != null && totals.Count > 0)
				{
					int num2 = rows.Count + 3;
					foreach (ExcelTotal current in totals)
					{
						excelWorksheet.Cells[num2, 1].Value = L(current.LabelKey);
						excelWorksheet.Cells[num2, 1].Style.Font.Bold = true;
						excelWorksheet.Cells[num2, 2].Value = current.Value;
						if (current.Value is decimal)
						{
							excelWorksheet.Cells[num2, 2].Style.Numberformat.Format = "#,##0.00";
						}
						num2++;
					}
				}
				for (int num3 = 1; num3 <= columns.Count; num3++)
				{
					excelWorksheet.Column(num3).AutoFit(8.0, 60.0);
				}
			});
		}

		protected string EnumText<TEnum>(TEnum value) where TEnum : struct
		{
			string name = typeof(TEnum).Name;
			TEnum val = value;
			return L(name + "_" + val);
		}

		protected bool IsCostHidden()
		{
			if (FeatureChecker != null && AbpSession.TenantId.HasValue)
			{
				return FeatureChecker.IsEnabled(AbpSession.TenantId.Value, "App.HideCostAndValues");
			}
			return false;
		}

		protected static ExcelTotal Total(string labelKey, object value, bool isCost = false)
		{
			return new ExcelTotal(labelKey, value, isCost);
		}

		protected static ExcelColumn<T> Col<T>(string headerKey, Func<T, object> value, string numberFormat = null, bool isCost = false)
		{
			return new ExcelColumn<T>(headerKey, value, numberFormat, isCost);
		}

		protected static ExcelColumn<T> CostCol<T>(string headerKey, Func<T, object> value, string numberFormat = "#,##0.00")
		{
			return new ExcelColumn<T>(headerKey, value, numberFormat, isCost: true);
		}

		protected static ExcelTotal CostTotal(string labelKey, object value)
		{
			return new ExcelTotal(labelKey, value, isCost: true);
		}

		protected static object DateOnly(DateTime? value)
		{
			return value?.Date;
		}

		private static string Truncate(string value, int length)
		{
			if (value.Length > length)
			{
				return value.Substring(0, length);
			}
			return value;
		}
	}
}
