using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("StockMovements")]
	public class StockMovement : FullAuditedEntity<long>, IMustHaveTenant
	{
		public virtual int TenantId { get; set; }

		public virtual int ItemMasterId { get; set; }

		[ForeignKey("ItemMasterId")]
		public virtual ItemMaster ItemMasterInfo { get; set; }

		public virtual DateTime MovementDate { get; set; }

		public virtual StockMovementType MovementType { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal UnitCost { get; set; }

		public virtual decimal TotalValue { get; set; }

		public virtual decimal BalanceAfter { get; set; }

		[StringLength(50)]
		public virtual string ReferenceType { get; set; }

		public virtual int? ReferenceId { get; set; }

		public virtual int? ReferenceItemId { get; set; }

		[StringLength(50)]
		public virtual string ReferenceNo { get; set; }

		public virtual string Remarks { get; set; }
	}
}
