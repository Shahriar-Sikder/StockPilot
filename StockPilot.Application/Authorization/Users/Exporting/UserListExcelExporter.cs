using System.Collections.Generic;
using System.Linq;
using Abp.Collections.Extensions;
using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using OfficeOpenXml;
using StockPilot.Authorization.Users.Dto;
using StockPilot.DataExporting.Excel.EpPlus;
using StockPilot.Dto;

namespace StockPilot.Authorization.Users.Exporting
{
	public class UserListExcelExporter : EpPlusExcelExporterBase, IUserListExcelExporter
	{
		private readonly ITimeZoneConverter _timeZoneConverter;

		private readonly IAbpSession _abpSession;

		public UserListExcelExporter(ITimeZoneConverter timeZoneConverter, IAbpSession abpSession)
		{
			_timeZoneConverter = timeZoneConverter;
			_abpSession = abpSession;
		}

		public FileDto ExportToFile(List<UserListDto> userListDtos)
		{
			return CreateExcelPackage("UserList.xlsx", delegate(ExcelPackage excelPackage)
			{
				ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add(L("Users"));
				excelWorksheet.OutLineApplyStyle = true;
				AddHeader(excelWorksheet, L("Name"), L("Surname"), L("UserName"), L("PhoneNumber"), L("EmailAddress"), L("EmailConfirm"), L("Roles"), L("LastLoginTime"), L("Active"), L("CreationTime"));
				AddObjects(excelWorksheet, 2, userListDtos, (UserListDto _) => _.Name, (UserListDto _) => _.Surname, (UserListDto _) => _.UserName, (UserListDto _) => _.PhoneNumber, (UserListDto _) => _.EmailAddress, (UserListDto _) => _.IsEmailConfirmed, (UserListDto _) => _.Roles.Select((UserListDto.UserListRoleDto r) => r.RoleName).JoinAsString(", "), (UserListDto _) => _timeZoneConverter.Convert(_.LastLoginTime, _abpSession.TenantId, _abpSession.GetUserId()), (UserListDto _) => _.IsActive, (UserListDto _) => _timeZoneConverter.Convert(_.CreationTime, _abpSession.TenantId, _abpSession.GetUserId()));
				excelWorksheet.Column(8).Style.Numberformat.Format = "yyyy-mm-dd";
				excelWorksheet.Column(10).Style.Numberformat.Format = "yyyy-mm-dd";
				for (int num = 1; num <= 10; num++)
				{
					excelWorksheet.Column(num).AutoFit();
				}
			});
		}
	}
}
