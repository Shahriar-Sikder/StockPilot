using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("Currencies")]
	public class Currency : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(10)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }

		[StringLength(10)]
		public virtual string Symbol { get; set; }

		public virtual int DecimalPlaces { get; set; } = 2;

		public virtual bool IsBaseCurrency { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual int Sequence { get; set; }
	}
}
