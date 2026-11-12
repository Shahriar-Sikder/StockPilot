using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("StockBalances")]
	public class StockBalance : FullAuditedEntity, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int ItemMasterId { get; set; }

		[ForeignKey("ItemMasterId")]
		public virtual ItemMaster ItemMasterInfo { get; set; }

		[Column(TypeName = "decimal")]
		public virtual decimal QuantityOnHand { get; set; }

		public virtual decimal ReservedQuantity { get; set; }

		public virtual decimal AverageCost { get; set; }

		public virtual decimal LastPurchaseCost { get; set; }

		public virtual DateTime? LastMovementTime { get; set; }

		[Timestamp]
		public virtual byte[] RowVersion { get; set; }

		[NotMapped]
		public virtual decimal AvailableQuantity => QuantityOnHand - ReservedQuantity;
	}
}
