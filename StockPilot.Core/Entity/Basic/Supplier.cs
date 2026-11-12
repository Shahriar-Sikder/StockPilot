using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("Suppliers")]
	public class Supplier : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(50)]
		public virtual string Code { get; set; }

		[Required]
		[StringLength(200)]
		public virtual string Name { get; set; }

		[StringLength(150)]
		public virtual string ContactPerson { get; set; }

		[StringLength(150)]
		public virtual string Email { get; set; }

		[StringLength(50)]
		public virtual string Phone { get; set; }

		[StringLength(500)]
		public virtual string Address { get; set; }

		[StringLength(100)]
		public virtual string City { get; set; }

		public virtual int? CountryId { get; set; }

		[ForeignKey("CountryId")]
		public virtual Country CountryInfo { get; set; }

		public virtual int? CurrencyId { get; set; }

		[ForeignKey("CurrencyId")]
		public virtual Currency CurrencyInfo { get; set; }

		[StringLength(50)]
		public virtual string TaxNumber { get; set; }

		public virtual int PaymentTermDays { get; set; }

		public virtual int Sequence { get; set; }

		public virtual bool IsActive { get; set; } = true;
	}
}
