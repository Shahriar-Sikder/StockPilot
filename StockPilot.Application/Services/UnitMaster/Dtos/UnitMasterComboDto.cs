using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitMaster.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.UnitMaster) })]
	public class UnitMasterComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public int UnitSetId { get; set; }

		public bool IsBaseUnit { get; set; }
	}
}
