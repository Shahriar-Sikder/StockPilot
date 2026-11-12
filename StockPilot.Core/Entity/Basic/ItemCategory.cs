using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("ItemCategories")]
	public class ItemCategory : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(150)]
		public virtual string Name { get; set; }

		[StringLength(500)]
		public virtual string Description { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual Guid? PictureId { get; set; }

		public virtual int Sequence { get; set; }

		public virtual ICollection<ItemSubCategory> SubCategories { get; set; }
	}
}
