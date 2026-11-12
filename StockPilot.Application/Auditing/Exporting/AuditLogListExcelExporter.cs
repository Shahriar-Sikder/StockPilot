using System.Collections.Generic;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using OfficeOpenXml;
using StockPilot.Auditing.Dto;
using StockPilot.DataExporting.Excel.EpPlus;
using StockPilot.Dto;

namespace StockPilot.Auditing.Exporting
{
	public class AuditLogListExcelExporter : EpPlusExcelExporterBase, IAuditLogListExcelExporter
	{
		private readonly ITimeZoneConverter _timeZoneConverter;

		private readonly IAbpSession _abpSession;

		public AuditLogListExcelExporter(ITimeZoneConverter timeZoneConverter, IAbpSession abpSession)
		{
			_timeZoneConverter = timeZoneConverter;
			_abpSession = abpSession;
		}

		public FileDto ExportToFile(List<AuditLogListDto> auditLogListDtos)
		{
			return CreateExcelPackage("AuditLogs.xlsx", delegate(ExcelPackage excelPackage)
			{
				ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add(L("AuditLogs"));
				excelWorksheet.OutLineApplyStyle = true;
				AddHeader(excelWorksheet, L("Time"), L("UserName"), L("Service"), L("Action"), L("Parameters"), L("Duration"), L("IpAddress"), L("Client"), L("Browser"), L("ErrorState"));
				AddObjects(excelWorksheet, 2, auditLogListDtos, (AuditLogListDto _) => _timeZoneConverter.Convert(_.ExecutionTime, _abpSession.TenantId, _abpSession.GetUserId()), (AuditLogListDto _) => _.UserName, (AuditLogListDto _) => _.ServiceName, (AuditLogListDto _) => _.MethodName, (AuditLogListDto _) => _.Parameters, (AuditLogListDto _) => _.ExecutionDuration, (AuditLogListDto _) => _.ClientIpAddress, (AuditLogListDto _) => _.ClientName, (AuditLogListDto _) => _.BrowserInfo, (AuditLogListDto _) => (!_.Exception.IsNullOrEmpty()) ? _.Exception : L("Success"));
				excelWorksheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
				for (int num = 1; num <= 10; num++)
				{
					if (!num.IsIn(5, 10))
					{
						excelWorksheet.Column(num).AutoFit();
					}
				}
			});
		}
	}
}
