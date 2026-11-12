using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.MultiTenancy;

namespace StockPilot.Services.TenantRequest.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.MultiTenancy.TenantRequest) })]
	public class TenantRequestListDto : EntityDto, IHasCreationTime
	{
		public string RequestNo { get; set; }

		public string StoreName { get; set; }

		public string TenancyName { get; set; }

		public string ContactName { get; set; }

		public string ContactPhone { get; set; }

		public string City { get; set; }

		public string AdminEmailAddress { get; set; }

		public TenantRequestStatus Status { get; set; }

		public DateTime? ReviewedTime { get; set; }

		public int? CreatedTenantId { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
