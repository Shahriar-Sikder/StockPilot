using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Country.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.Country) })]
	public class CountryComboDto : EntityDto
	{
		public string Code { get; set; }

		public string Name { get; set; }
	}
}
