using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemCategory.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.ItemCategory) })]
	public class EditItemCategoryDto : CreationAuditedEntityDto
	{
		[Required]
		[StringLength(50)]
		public string Code { get; set; }

		[Required]
		[StringLength(150)]
		public string Name { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }
	}
}
