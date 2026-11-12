using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.MultiTenancy
{
	public class TenantRequest : FullAuditedEntity
	{
		public const int MaxRequestNoLength = 32;

		public const int MaxReviewNoteLength = 1000;

		public const int MaxIpAddressLength = 64;

		[Required]
		[MaxLength(32)]
		public virtual string RequestNo { get; set; }

		[Required]
		[MaxLength(64)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_-]{1,}$")]
		public virtual string TenancyName { get; set; }

		[Required]
		[MaxLength(128)]
		public virtual string StoreName { get; set; }

		[MaxLength(128)]
		public virtual string ContactName { get; set; }

		[MaxLength(256)]
		public virtual string ContactEmail { get; set; }

		[MaxLength(32)]
		public virtual string ContactPhone { get; set; }

		[MaxLength(512)]
		public virtual string Address { get; set; }

		[MaxLength(128)]
		public virtual string City { get; set; }

		[MaxLength(128)]
		public virtual string CountryName { get; set; }

		[MaxLength(64)]
		public virtual string BusinessType { get; set; }

		[MaxLength(2000)]
		public virtual string Notes { get; set; }

		[Required]
		[MaxLength(32)]
		public virtual string AdminName { get; set; }

		[Required]
		[MaxLength(32)]
		public virtual string AdminSurname { get; set; }

		[Required]
		[MaxLength(256)]
		public virtual string AdminEmailAddress { get; set; }

		[MaxLength(24)]
		public virtual string AdminPhoneNumber { get; set; }

		[MaxLength(128)]
		public virtual string AdminPasswordHash { get; set; }

		public virtual int? RequestedEditionId { get; set; }

		public virtual TenantRequestStatus Status { get; set; }

		public virtual long? ReviewedByUserId { get; set; }

		public virtual DateTime? ReviewedTime { get; set; }

		[MaxLength(1000)]
		public virtual string ReviewNote { get; set; }

		public virtual int? CreatedTenantId { get; set; }

		[MaxLength(64)]
		public virtual string RequesterIpAddress { get; set; }
	}
}
