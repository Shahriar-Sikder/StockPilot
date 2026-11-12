using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemSubCategory.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.ItemSubCategory) })]
	public class ItemSubCategoryComboDto : EntityDto
	{
		public int ItemCategoryId { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }
	}
}
