using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("IssueSales")]
	public class IssueSale : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string DocumentNo { get; set; }

		public virtual DateTime DocumentDate { get; set; }

		public virtual IssueSaleType IssueType { get; set; } = IssueSaleType.Sale;

		[StringLength(200)]
		public virtual string CustomerName { get; set; }

		[StringLength(100)]
		public virtual string ReferenceNo { get; set; }

		public virtual int CurrencyId { get; set; }

		[ForeignKey("CurrencyId")]
		public virtual Currency CurrencyInfo { get; set; }

		public virtual decimal ExchangeRate { get; set; } = 1m;

		public virtual decimal SubTotal { get; set; }

		public virtual decimal DiscountAmount { get; set; }

		public virtual decimal TaxAmount { get; set; }

		public virtual decimal TotalAmount { get; set; }

		public virtual DocumentStatus Status { get; set; }

		public virtual bool IsPosted { get; set; }

		public virtual long? PostedBy { get; set; }

		public virtual DateTime? PostedTime { get; set; }

		[StringLength(1000)]
		public virtual string Remarks { get; set; }

		public virtual ICollection<IssueSalesItem> Items { get; set; } = new List<IssueSalesItem>();
	}
}
