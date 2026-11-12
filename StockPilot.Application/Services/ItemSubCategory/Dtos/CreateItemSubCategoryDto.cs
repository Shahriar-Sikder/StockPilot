using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemSubCategory.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.ItemSubCategory) })]
	public class CreateItemSubCategoryDto : FullAuditedEntityDto
	{
		[Range(1, int.MaxValue)]
		public int ItemCategoryId { get; set; }

		[Required]
		[StringLength(50)]
		public string Code { get; set; }

		[Required]
		[StringLength(150)]
		public string Name { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		public int Sequence { get; set; }

		public bool IsActive { get; set; } = true;
	}
}
