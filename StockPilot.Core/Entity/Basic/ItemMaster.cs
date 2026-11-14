using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("ItemMasters")]
	public class ItemMaster : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string ItemCode { get; set; }

		[Required]
		[StringLength(250)]
		public virtual string ItemName { get; set; }

		[StringLength(1000)]
		public virtual string Description { get; set; }

		[StringLength(100)]
		public virtual string Barcode { get; set; }

		public virtual int ItemCategoryId { get; set; }

		[ForeignKey("ItemCategoryId")]
		public virtual ItemCategory ItemCategoryInfo { get; set; }

		public virtual int? ItemSubCategoryId { get; set; }

		[ForeignKey("ItemSubCategoryId")]
		public virtual ItemSubCategory ItemSubCategoryInfo { get; set; }

		public virtual int UnitSetId { get; set; }

		[ForeignKey("UnitSetId")]
		public virtual UnitSet UnitSetInfo { get; set; }

		public virtual int BaseUnitId { get; set; }

		[ForeignKey("BaseUnitId")]
		public virtual UnitMaster BaseUnit { get; set; }

		public virtual int? PurchaseUnitId { get; set; }

		[ForeignKey("PurchaseUnitId")]
		public virtual UnitMaster PurchaseUnit { get; set; }

		public virtual int? SalesUnitId { get; set; }

		[ForeignKey("SalesUnitId")]
		public virtual UnitMaster SalesUnit { get; set; }

		public virtual int? DefaultSupplierId { get; set; }

		[ForeignKey("DefaultSupplierId")]
		public virtual Supplier DefaultSupplierInfo { get; set; }

		public virtual decimal StandardCost { get; set; }

		public virtual decimal SalePrice { get; set; }

		public virtual decimal TaxPercent { get; set; }

		public virtual decimal ReorderLevel { get; set; }

		public virtual decimal MinStock { get; set; }

		public virtual decimal MaxStock { get; set; }

		public virtual bool IsStockItem { get; set; } = true;

		public virtual bool IsActive { get; set; } = true;

		/// <summary>Old storage: the picture as a row in AppBinaryObjects. Moved to <see cref="PictureUrl"/> by
		/// PictureFileMigrator, then dropped by a later migration.</summary>
		public virtual Guid? PictureId { get; set; }

		/// <summary>The picture file, as a relative URL such as /Pictures/t3/items/{guid}.png (PictureFileStore).</summary>
		[StringLength(500)]
		public virtual string PictureUrl { get; set; }
	}
}
