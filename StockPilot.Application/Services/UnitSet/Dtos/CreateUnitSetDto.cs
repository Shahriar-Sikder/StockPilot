using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitSet.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.UnitSet) })]
	public class CreateUnitSetDto : FullAuditedEntityDto
	{
		[Required]
		[StringLength(50)]
		public string Code { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		public bool IsActive { get; set; } = true;
	}
}
