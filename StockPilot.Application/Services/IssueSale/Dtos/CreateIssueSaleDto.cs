using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.IssueSale.Dtos
{
	[AutoMapTo(new Type[] { typeof(StockPilot.Entity.IssueSale) })]
	public class CreateIssueSaleDto : FullAuditedEntityDto
	{
		public DateTime DocumentDate { get; set; }

		public IssueSaleType IssueType { get; set; } = IssueSaleType.Sale;

		[StringLength(200)]
		public string CustomerName { get; set; }

		[StringLength(100)]
		public string ReferenceNo { get; set; }

		[Range(1, int.MaxValue)]
		public int CurrencyId { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal ExchangeRate { get; set; }

		[Range(0.0, double.MaxValue)]
		public decimal TaxAmount { get; set; }

		[StringLength(1000)]
		public string Remarks { get; set; }

		public List<CreateIssueSalesItemDto> Lines { get; set; } = new List<CreateIssueSalesItemDto>();
	}
}
