using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.Country.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.Country) })]
	public class EditCountryDto : CreationAuditedEntityDto
	{
		[Required]
		[StringLength(10)]
		public string Code { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[StringLength(10)]
		public string PhoneCode { get; set; }

		public bool IsActive { get; set; }

		public int Sequence { get; set; }
	}
}
