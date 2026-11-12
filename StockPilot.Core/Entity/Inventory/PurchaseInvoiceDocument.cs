using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using StockPilot.Storage;

namespace StockPilot.Entity
{
	[Table("PurchaseInvoiceDocuments")]
	public class PurchaseInvoiceDocument : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int PurchaseInvoiceId { get; set; }

		[ForeignKey("PurchaseInvoiceId")]
		public virtual PurchaseInvoice PurchaseInvoiceInfo { get; set; }

		[Required]
		[StringLength(255)]
		public virtual string FileName { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string ContentType { get; set; }

		public virtual long SizeBytes { get; set; }

		public virtual Guid BinaryObjectId { get; set; }

		[ForeignKey("BinaryObjectId")]
		public virtual BinaryObject BinaryObjectInfo { get; set; }
	}
}
