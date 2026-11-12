using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitConversion.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.UnitConversion) })]
	public class UnitConversionComboDto : EntityDto
	{
		public int FromUnitId { get; set; }

		public string FromUnitCode { get; set; }

		public int ToUnitId { get; set; }

		public string ToUnitCode { get; set; }

		public decimal Factor { get; set; }

		public int UnitSetId { get; set; }
	}
}
