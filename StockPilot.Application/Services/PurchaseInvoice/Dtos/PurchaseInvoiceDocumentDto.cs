using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.PurchaseInvoice.Dtos
{
	[AutoMapFrom(new Type[] { typeof(PurchaseInvoiceDocument) })]
	public class PurchaseInvoiceDocumentDto : EntityDto
	{
		public int PurchaseInvoiceId { get; set; }

		public string FileName { get; set; }

		public string ContentType { get; set; }

		public long SizeBytes { get; set; }

		public DateTime CreationTime { get; set; }

		public long? CreatorUserId { get; set; }
	}
}
