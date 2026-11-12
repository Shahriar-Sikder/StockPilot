using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Currency.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.Currency) })]
	public class CurrencyComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string Symbol { get; set; }

		public int DecimalPlaces { get; set; }

		public bool IsBaseCurrency { get; set; }
	}
}
