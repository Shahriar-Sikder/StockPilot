using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.CurrencyConversion.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.CurrencyConversion) })]
	public class CreateCurrencyConversionDto : FullAuditedEntityDto
	{
		[Range(1, int.MaxValue)]
		public int FromCurrencyId { get; set; }

		[Range(1, int.MaxValue)]
		public int ToCurrencyId { get; set; }

		[Range(typeof(decimal), "0.000001", "999999999999")]
		public decimal Rate { get; set; }

		public DateTime EffectiveDate { get; set; }

		public bool IsActive { get; set; } = true;

		public int Sequence { get; set; }
	}
}
