using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("PurchaseInvoiceItems")]
	public class PurchaseInvoiceItem : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int PurchaseInvoiceId { get; set; }

		[ForeignKey("PurchaseInvoiceId")]
		public virtual PurchaseInvoice PurchaseInvoiceInfo { get; set; }

		public virtual int ItemMasterId { get; set; }

		[ForeignKey("ItemMasterId")]
		public virtual ItemMaster ItemMasterInfo { get; set; }

		public virtual int UnitMasterId { get; set; }

		[ForeignKey("UnitMasterId")]
		public virtual UnitMaster UnitMasterInfo { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal ConversionFactor { get; set; } = 1m;

		public virtual decimal BaseQuantity { get; set; }

		public virtual DateTime? ExpiryDate { get; set; }

		public virtual decimal UnitCost { get; set; }

		public virtual decimal DiscountPercent { get; set; }

		public virtual decimal TaxPercent { get; set; }

		public virtual decimal TaxAmount { get; set; }

		public virtual decimal DiscountAmount { get; set; }

		public virtual decimal LineTotal { get; set; }

		[StringLength(500)]
		public virtual string Remarks { get; set; }
	}
}
