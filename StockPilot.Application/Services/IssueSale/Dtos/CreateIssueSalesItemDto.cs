using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.IssueSale.Dtos
{
	[AutoMapTo(new Type[] { typeof(IssueSalesItem) })]
	public class CreateIssueSalesItemDto : FullAuditedEntityDto
	{
		[Range(1, int.MaxValue)]
		public int ItemMasterId { get; set; }

		[Range(1, int.MaxValue)]
		public int UnitMasterId { get; set; }

		[Range(typeof(decimal), "0.0001", "999999999999")]
		public decimal Quantity { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal UnitPrice { get; set; }

		[Range(0, 100)]
		public decimal DiscountPercent { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal DiscountAmount { get; set; }

		[StringLength(500)]
		public string Remarks { get; set; }
	}
}
