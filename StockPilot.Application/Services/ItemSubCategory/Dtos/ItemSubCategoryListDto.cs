using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.ItemSubCategory.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.ItemSubCategory) })]
	public class ItemSubCategoryListDto : EntityDto, IHasCreationTime
	{
		public int ItemCategoryId { get; set; }

		public string ItemCategoryName { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public int Sequence { get; set; }

		public bool IsActive { get; set; }

		public Guid? PictureId { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
