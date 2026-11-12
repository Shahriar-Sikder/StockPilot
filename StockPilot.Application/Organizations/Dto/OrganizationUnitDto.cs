using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Organizations;

namespace StockPilot.Organizations.Dto
{
	[AutoMapFrom(new Type[] { typeof(OrganizationUnit) })]
	public class OrganizationUnitDto : AuditedEntityDto<long>
	{
		public long? ParentId { get; set; }

		public string Code { get; set; }

		public string DisplayName { get; set; }

		public int MemberCount { get; set; }
	}
}
