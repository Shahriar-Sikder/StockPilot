using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.UnitSet.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.UnitSet) })]
	public class UnitSetListDto : EntityDto, IHasCreationTime
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public bool IsActive { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
