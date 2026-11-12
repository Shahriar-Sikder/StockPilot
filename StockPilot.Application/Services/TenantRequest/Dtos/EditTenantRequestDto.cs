using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.MultiTenancy;

namespace StockPilot.Services.TenantRequest.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.MultiTenancy.TenantRequest) })]
	public class EditTenantRequestDto : CreationAuditedEntityDto
	{
		[Required]
		[StringLength(64)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_-]{1,}$")]
		public string TenancyName { get; set; }

		[Required]
		[StringLength(128)]
		public string StoreName { get; set; }

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

		[Required]
		[StringLength(32)]
		public string AdminName { get; set; }

		[Required]
		[StringLength(32)]
		public string AdminSurname { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(256)]
		public string AdminEmailAddress { get; set; }

		[StringLength(24)]
		public string AdminPhoneNumber { get; set; }

		public int? RequestedEditionId { get; set; }

		public string RequestNo { get; set; }

		public TenantRequestStatus Status { get; set; }

		public long? ReviewedByUserId { get; set; }

		public DateTime? ReviewedTime { get; set; }

		public string ReviewNote { get; set; }

		public int? CreatedTenantId { get; set; }

		public string RequesterIpAddress { get; set; }
	}
}
