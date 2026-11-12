using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitConversion.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.UnitConversion) })]
	public class EditUnitConversionDto : CreationAuditedEntityDto
	{
		[Range(1, int.MaxValue)]
		public int FromUnitId { get; set; }

		[Range(1, int.MaxValue)]
		public int ToUnitId { get; set; }

		[Range(typeof(decimal), "0.000001", "999999999999")]
		public decimal Factor { get; set; }

		public int UnitSetId { get; set; }

		public bool IsActive { get; set; }
	}
}
