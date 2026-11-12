using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Editions;
using Abp.AutoMapper;

namespace StockPilot.Editions.Dto
{
	[AutoMap(new Type[] { typeof(Edition) })]
	public class EditionEditDto
	{
		public int? Id { get; set; }

		[Required]
		public string DisplayName { get; set; }
	}
}
