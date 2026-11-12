using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Supplier.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.Supplier) })]
	public class SupplierComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public int? CurrencyId { get; set; }

		public int PaymentTermDays { get; set; }
	}
}
