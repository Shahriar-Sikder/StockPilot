using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.UnitSet.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.UnitSet) })]
	public class UnitSetComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }
	}
}
