using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("Countries")]
	public class Country : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(10)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }

		[StringLength(10)]
		public virtual string PhoneCode { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual int Sequence { get; set; }
	}
}
