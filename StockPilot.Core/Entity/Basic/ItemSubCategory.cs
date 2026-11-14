using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("ItemSubCategories")]
	public class ItemSubCategory : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int ItemCategoryId { get; set; }

		[ForeignKey("ItemCategoryId")]
		public virtual ItemCategory ItemCategoryInfo { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(150)]
		public virtual string Name { get; set; }

		[StringLength(500)]
		public virtual string Description { get; set; }

		public virtual int Sequence { get; set; }

		public virtual bool IsActive { get; set; } = true;

		/// <summary>Old storage: the picture as a row in AppBinaryObjects. Moved to <see cref="PictureUrl"/> by
		/// PictureFileMigrator, then dropped by a later migration.</summary>
		public virtual Guid? PictureId { get; set; }

		/// <summary>The picture file, as a relative URL such as /Pictures/t3/items/{guid}.png (PictureFileStore).</summary>
		[StringLength(500)]
		public virtual string PictureUrl { get; set; }
	}
}
