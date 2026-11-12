using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Abp.UI;
using Excel;

namespace StockPilot.Web.Utility
{
	public class ExcelData
	{
		private readonly string _path;

		private readonly Stream _stream;

		public ExcelData(string path, Stream stream)
		{
			_path = path;
			_stream = stream;
		}

		private IExcelDataReader GetExcelReader()
		{
			IExcelDataReader reader = null;
			try
			{
				if (_path.EndsWith(".xls"))
				{
					reader = ExcelReaderFactory.CreateBinaryReader(_stream);
				}
				if (_path.EndsWith(".xlsx"))
				{
					reader = ExcelReaderFactory.CreateOpenXmlReader(_stream);
				}
				return reader;
			}
			catch (Exception)
			{
				throw;
			}
		}

		private DataSet ConvertToDataSetOfStrings(DataSet sourceDataSet)
		{
			DataSet dataSet = new DataSet();
			dataSet.Tables.AddRange(sourceDataSet.Tables.Cast<DataTable>().Select(delegate(DataTable srcDataTable)
			{
				DataTable destDataTable = new DataTable(srcDataTable.TableName, srcDataTable.Namespace);
				destDataTable.Columns.AddRange((from DataColumn col in srcDataTable.Columns
					select new DataColumn(col.ColumnName, typeof(string))).ToArray());
				srcDataTable.Rows.OfType<DataRow>().ToList().ForEach(delegate(DataRow row)
				{
					destDataTable.ImportRow(row);
				});
				return destDataTable;
			}).ToArray());
			return dataSet;
		}

		public IEnumerable<DataRow> GetDataAsSet(bool firstRowIsColumnNames = true)
		{
			try
			{
				IExcelDataReader excelReader = GetExcelReader();
				DataSet workbook = excelReader.AsDataSet();
				workbook = ConvertToDataSetOfStrings(workbook);
				IEnumerable<string> sheets = from DataTable sheet in workbook.Tables
					select sheet.TableName;
				excelReader.IsFirstRowAsColumnNames = firstRowIsColumnNames;
				return from DataRow a in (excelReader.AsDataSet().Tables[sheets.First()] ?? throw new UserFriendlyException("Records Not Found")).Rows
					select (a);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public IEnumerable<DataRow> GetData(bool firstRowIsColumnNames = true)
		{
			try
			{
				IExcelDataReader excelReader = GetExcelReader();
				IEnumerable<string> sheets = from DataTable sheet in excelReader.AsDataSet().Tables
					select sheet.TableName;
				excelReader.IsFirstRowAsColumnNames = firstRowIsColumnNames;
				return from DataRow a in (excelReader.AsDataSet().Tables[sheets.First()] ?? throw new UserFriendlyException("Records Not Found")).Rows
					select (a);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}
