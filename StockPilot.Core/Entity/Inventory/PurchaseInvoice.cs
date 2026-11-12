using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("PurchaseInvoices")]
	public class PurchaseInvoice : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string InvoiceNo { get; set; }

		public virtual DateTime InvoiceDate { get; set; }

		public virtual int SupplierId { get; set; }

		[ForeignKey("SupplierId")]
		public virtual Supplier SupplierInfo { get; set; }

		[StringLength(50)]
		public virtual string SupplierInvoiceNo { get; set; }

		public virtual DateTime? DueDate { get; set; }

		public virtual int CurrencyId { get; set; }

		[ForeignKey("CurrencyId")]
		public virtual Currency CurrencyInfo { get; set; }

		public virtual decimal ExchangeRate { get; set; } = 1m;

		public virtual decimal SubTotal { get; set; }

		public virtual decimal DiscountAmount { get; set; }

		public virtual decimal TaxAmount { get; set; }

		public virtual decimal OtherCharges { get; set; }

		public virtual decimal TotalAmount { get; set; }

		public virtual DocumentStatus Status { get; set; }

		public virtual bool IsPosted { get; set; }

		public virtual long? PostedBy { get; set; }

		public virtual DateTime? PostedTime { get; set; }

		public virtual string Remarks { get; set; }

		public virtual ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();

		public virtual ICollection<PurchaseInvoiceDocument> Documents { get; set; } = new List<PurchaseInvoiceDocument>();
	}
}
