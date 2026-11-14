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

		/// <summary>Old storage: the document as a row in AppBinaryObjects. New documents are files (<see cref="FilePath"/>);
		/// old ones are moved by PictureFileMigrator, then this column can be dropped.</summary>
		public virtual Guid? BinaryObjectId { get; set; }

		/// <summary>The document file, relative to the picture folder: documents/t{tenantId}/{guid}.{ext} (DocumentFileStore).
		/// Private: only downloaded through PurchaseInvoice.DownloadDocument.</summary>
		[StringLength(500)]
		public virtual string FilePath { get; set; }

		[ForeignKey("BinaryObjectId")]
		public virtual BinaryObject BinaryObjectInfo { get; set; }
	}
}
