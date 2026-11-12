using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.Supplier.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.Supplier) })]
	public class SupplierListDto : EntityDto, IHasCreationTime
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string ContactPerson { get; set; }

		public string Email { get; set; }

		public string Phone { get; set; }

		public string City { get; set; }

		public int? CountryId { get; set; }

		public string CountryName { get; set; }

		public int? CurrencyId { get; set; }

		public string CurrencyCode { get; set; }

		public int PaymentTermDays { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
