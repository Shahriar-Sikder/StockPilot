using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.MultiTenancy.Dto
{
	[AutoMapFrom(new Type[] { typeof(Tenant) })]
	public class TenantListDto : EntityDto, IPassivable, IHasCreationTime
	{
		public string TenancyName { get; set; }

		public string Name { get; set; }

		public int? EditionId { get; set; }

		public string EditionDisplayName { get; set; }

		public string ConnectionString { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreationTime { get; set; }

		public string ContactName { get; set; }

		public string ContactEmail { get; set; }

		public string ContactPhone { get; set; }

		public string Address { get; set; }

		public string City { get; set; }

		public string CountryName { get; set; }

		public string BusinessType { get; set; }

		public string Notes { get; set; }

		public DateTime? SubscriptionEndDate { get; set; }

		public bool IsSubscriptionExpired { get; set; }

		public TenantSubscriptionStatus SubscriptionStatus { get; set; }

		public int UserCount { get; set; }

		public int FeatureOverrideCount { get; set; }
	}
}
