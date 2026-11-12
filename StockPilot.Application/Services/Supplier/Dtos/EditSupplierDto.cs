using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Supplier.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.Supplier) })]
	public class EditSupplierDto : CreationAuditedEntityDto
	{
		[Required]
		[StringLength(50)]
		public string Code { get; set; }

		[Required]
		[StringLength(200)]
		public string Name { get; set; }

		[StringLength(150)]
		public string ContactPerson { get; set; }

		[StringLength(150)]
		[EmailAddress]
		public string Email { get; set; }

		[StringLength(50)]
		public string Phone { get; set; }

		[StringLength(500)]
		public string Address { get; set; }

		[StringLength(100)]
		public string City { get; set; }

		public int? CountryId { get; set; }

		public int? CurrencyId { get; set; }

		[StringLength(50)]
		public string TaxNumber { get; set; }

		[Range(0, 3650)]
		public int PaymentTermDays { get; set; }

		public int Sequence { get; set; }

		public bool IsActive { get; set; }
	}
}
