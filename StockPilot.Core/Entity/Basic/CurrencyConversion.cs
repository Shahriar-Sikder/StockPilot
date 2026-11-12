using System;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("CurrencyConversions")]
	public class CurrencyConversion : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int FromCurrencyId { get; set; }

		[ForeignKey("FromCurrencyId")]
		public virtual Currency FromCurrency { get; set; }

		public virtual int ToCurrencyId { get; set; }

		[ForeignKey("ToCurrencyId")]
		public virtual Currency ToCurrency { get; set; }

		public virtual decimal Rate { get; set; }

		public virtual DateTime EffectiveDate { get; set; }

		public virtual bool IsActive { get; set; } = true;

		public virtual int Sequence { get; set; }
	}
}
