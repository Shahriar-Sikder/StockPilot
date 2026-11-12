using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Domain.Entities.Auditing;
using StockPilot.Entity;

namespace StockPilot.Services.StockMovement.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.StockMovement) })]
	public class StockMovementListDto : EntityDto<long>, IHasCreationTime
	{
		public DateTime MovementDate { get; set; }

		public int ItemMasterId { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string BaseUnitCode { get; set; }

		public StockMovementType MovementType { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitCost { get; set; }

		public decimal TotalValue { get; set; }

		public decimal BalanceAfter { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public string ReferenceNo { get; set; }

		public string Remarks { get; set; }

		public long? CreatorUserId { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
