using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("UnitConversions")]
	public class UnitConversion : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int FromUnitId { get; set; }

		[ForeignKey("FromUnitId")]
		public virtual UnitMaster FromUnit { get; set; }

		public virtual int ToUnitId { get; set; }

		[ForeignKey("ToUnitId")]
		public virtual UnitMaster ToUnit { get; set; }

		public virtual decimal Factor { get; set; }

		public virtual int UnitSetId { get; set; }

		[ForeignKey("UnitSetId")]
		public virtual UnitSet UnitSetInfo { get; set; }

		public virtual bool IsActive { get; set; } = true;
	}
}
