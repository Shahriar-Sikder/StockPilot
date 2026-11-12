using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.Currency.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.Currency) })]
	public class CurrencyListDto : EntityDto, IHasCreationTime
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string Symbol { get; set; }

		public int DecimalPlaces { get; set; }

		public bool IsBaseCurrency { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
