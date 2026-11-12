using System.Collections.Generic;
using StockPilot.Auditing.Dto;
using StockPilot.Dto;

namespace StockPilot.Auditing.Exporting
{
	public interface IAuditLogListExcelExporter
	{
		FileDto ExportToFile(List<AuditLogListDto> auditLogListDtos);
	}
}
