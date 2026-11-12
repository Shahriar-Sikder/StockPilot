using System;
using System.ComponentModel.DataAnnotations;
using Abp.Auditing;

namespace StockPilot.MultiTenancy.Dto
{
	public class CreateTenantInput
	{
		[Required]
		[StringLength(64)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_-]{1,}$")]
		public string TenancyName { get; set; }

		[Required]
		[StringLength(128)]
		public string Name { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(256)]
		public string AdminEmailAddress { get; set; }

		[StringLength(32)]
		public string AdminName { get; set; }

		[StringLength(32)]
		public string AdminSurname { get; set; }

		[StringLength(24)]
		[Phone]
		public string AdminPhoneNumber { get; set; }

		[StringLength(128)]
		[DisableAuditing]
		public string AdminPassword { get; set; }

		[MaxLength(1024)]
		public string ConnectionString { get; set; }

		public bool ShouldChangePasswordOnNextLogin { get; set; }

		public bool SendActivationEmail { get; set; }

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
	}
}
