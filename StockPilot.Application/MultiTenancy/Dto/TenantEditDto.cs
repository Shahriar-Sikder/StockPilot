using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace StockPilot.MultiTenancy.Dto
{
	[AutoMap(new Type[] { typeof(Tenant) })]
	public class TenantEditDto : EntityDto
	{
		[Required]
		[StringLength(64)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_-]{1,}$")]
		public string TenancyName { get; set; }

		[Required]
		[StringLength(128)]
		public string Name { get; set; }

		public string ConnectionString { get; set; }

		public int? EditionId { get; set; }

		public bool IsActive { get; set; }

		public DateTime? SubscriptionEndDate { get; set; }

		[StringLength(128)]
		public string ContactName { get; set; }

		[EmailAddress]
		[StringLength(256)]
		public string ContactEmail { get; set; }

		[StringLength(32)]
		public string ContactPhone { get; set; }

		[StringLength(512)]
		public string Address { get; set; }

		[StringLength(128)]
		public string City { get; set; }

		[StringLength(128)]
		public string CountryName { get; set; }

		[StringLength(64)]
		public string BusinessType { get; set; }

		[StringLength(2000)]
		public string Notes { get; set; }

		public string EditionDisplayName { get; set; }

		public DateTime CreationTime { get; set; }

		public bool IsSubscriptionExpired { get; set; }

		public TenantSubscriptionStatus SubscriptionStatus { get; set; }

		public int UserCount { get; set; }

		public int FeatureOverrideCount { get; set; }
	}
}
