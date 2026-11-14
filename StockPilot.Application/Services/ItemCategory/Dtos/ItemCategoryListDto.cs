using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.ItemCategory.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.ItemCategory) })]
	public class ItemCategoryListDto : EntityDto, IHasCreationTime
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }

		public string PictureUrl { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
