using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemCategory.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.ItemCategory) })]
	public class ItemCategoryComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }
	}
}
