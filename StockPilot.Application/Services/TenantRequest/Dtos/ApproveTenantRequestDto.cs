using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.TenantRequest.Dtos
{
	public class ApproveTenantRequestDto : EntityDto
	{
		public int? EditionId { get; set; }

		public bool IsActive { get; set; } = true;

		public DateTime? SubscriptionEndDate { get; set; }

		[StringLength(1000)]
		public string ReviewNote { get; set; }

		public List<NameValueDto> FeatureOverrides { get; set; } = new List<NameValueDto>();
	}
}
