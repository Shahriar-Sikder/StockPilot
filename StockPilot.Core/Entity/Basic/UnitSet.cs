using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("UnitSets")]
	public class UnitSet : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }

		[StringLength(500)]
		public virtual string Description { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual ICollection<UnitMaster> Units { get; set; }
	}
}
