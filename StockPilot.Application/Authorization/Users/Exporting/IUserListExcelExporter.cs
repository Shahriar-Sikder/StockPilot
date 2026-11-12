using System.Collections.Generic;
using StockPilot.Authorization.Users.Dto;
using StockPilot.Dto;

namespace StockPilot.Authorization.Users.Exporting
{
	public interface IUserListExcelExporter
	{
		FileDto ExportToFile(List<UserListDto> userListDtos);
	}
}
