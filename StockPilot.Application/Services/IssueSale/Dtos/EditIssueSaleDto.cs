using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.IssueSale.Dtos
{
	[AutoMap(new Type[] { typeof(StockPilot.Entity.IssueSale) })]
	public class EditIssueSaleDto : CreationAuditedEntityDto
	{
		public DateTime DocumentDate { get; set; }

		public IssueSaleType IssueType { get; set; }

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

		public List<EditIssueSalesItemDto> Lines { get; set; } = new List<EditIssueSalesItemDto>();

		public string DocumentNo { get; set; }

		public string CurrencyCode { get; set; }

		public DocumentStatus Status { get; set; }

		public bool IsPosted { get; set; }

		public DateTime? PostedTime { get; set; }

		public decimal SubTotal { get; set; }

		public decimal DiscountAmount { get; set; }

		public decimal TotalAmount { get; set; }

		public decimal CostTotal { get; set; }
	}
}
