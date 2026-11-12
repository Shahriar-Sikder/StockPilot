using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.CurrencyConversion.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.CurrencyConversion) })]
	public class CurrencyConversionComboDto : EntityDto
	{
		public int FromCurrencyId { get; set; }

		public string FromCurrencyCode { get; set; }

		public int ToCurrencyId { get; set; }

		public string ToCurrencyCode { get; set; }

		public decimal Rate { get; set; }

		public DateTime EffectiveDate { get; set; }
	}
}
