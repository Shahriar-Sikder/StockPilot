using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemMaster.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.StockMovement) })]
	public class ItemMovementDto : EntityDto<long>
	{
		public DateTime MovementDate { get; set; }

		public StockMovementType MovementType { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitCost { get; set; }

		public decimal BalanceAfter { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public string ReferenceNo { get; set; }

		public string Remarks { get; set; }

		public DateTime CreationTime { get; set; }
	}
}
