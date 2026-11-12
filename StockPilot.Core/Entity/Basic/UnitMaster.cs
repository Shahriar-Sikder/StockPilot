using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("UnitMasters")]
	public class UnitMaster : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(20)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }

		public virtual int UnitSetId { get; set; }

		[ForeignKey("UnitSetId")]
		public virtual UnitSet UnitSetInfo { get; set; }

		public virtual bool IsBaseUnit { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual int Sequence { get; set; }
	}
}
