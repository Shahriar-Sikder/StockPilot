using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("IssueSalesItems")]
	public class IssueSalesItem : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int IssueSaleId { get; set; }

		[ForeignKey("IssueSaleId")]
		public virtual IssueSale IssueSaleInfo { get; set; }

		public virtual int ItemMasterId { get; set; }

		[ForeignKey("ItemMasterId")]
		public virtual ItemMaster ItemMasterInfo { get; set; }

		public virtual int UnitMasterId { get; set; }

		[ForeignKey("UnitMasterId")]
		public virtual UnitMaster UnitMasterInfo { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal ConversionFactor { get; set; } = 1m;

		public virtual decimal BaseQuantity { get; set; }

		public virtual decimal UnitPrice { get; set; }

		public virtual decimal UnitCost { get; set; }

		public virtual decimal DiscountPercent { get; set; }

		public virtual decimal DiscountAmount { get; set; }

		public virtual decimal LineTotal { get; set; }

		[StringLength(500)]
		public virtual string Remarks { get; set; }
	}
}
