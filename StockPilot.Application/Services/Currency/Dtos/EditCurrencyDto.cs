using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Currency.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.Currency) })]
	public class EditCurrencyDto : CreationAuditedEntityDto
	{
		[Required]
		[StringLength(10)]
		public string Code { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[StringLength(10)]
		public string Symbol { get; set; }

		[Range(0, 6)]
		public int DecimalPlaces { get; set; }

		public bool IsBaseCurrency { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }
	}
}
