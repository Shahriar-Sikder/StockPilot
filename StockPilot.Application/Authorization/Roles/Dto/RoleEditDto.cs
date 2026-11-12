using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace StockPilot.Authorization.Roles.Dto
{
	[AutoMap(new Type[] { typeof(Role) })]
	public class RoleEditDto
	{
		public int? Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		public bool IsDefault { get; set; }
	}
}
