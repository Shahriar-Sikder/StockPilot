using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.UnitMaster.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.UnitMaster) })]
	public class UnitMasterListDto : EntityDto, IHasCreationTime
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public int UnitSetId { get; set; }

		public string UnitSetName { get; set; }

		public bool IsBaseUnit { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
