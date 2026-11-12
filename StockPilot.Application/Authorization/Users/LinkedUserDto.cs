using System;
using Abp.Application.Services.Dto;

namespace StockPilot.Authorization.Users
{
	public class LinkedUserDto : EntityDto<long>
	{
		public int? TenantId { get; set; }

		public string TenancyName { get; set; }

		public string Username { get; set; }

		public DateTime? LastLoginTime { get; set; }

		public object GetShownLoginName(bool multiTenancyEnabled)
		{
			if (!multiTenancyEnabled)
			{
				return Username;
			}
			if (!string.IsNullOrEmpty(TenancyName))
			{
				return TenancyName + "\\" + Username;
			}
			return ".\\" + Username;
		}
	}
}
