using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitMaster.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.UnitMaster) })]
	public class CreateUnitMasterDto : FullAuditedEntityDto
	{
		[Required]
		[StringLength(20)]
		public string Code { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Range(1, int.MaxValue)]
		public int UnitSetId { get; set; }

		public bool IsBaseUnit { get; set; }

		public bool IsActive { get; set; } = true;

		public int Sequence { get; set; }
	}
}
