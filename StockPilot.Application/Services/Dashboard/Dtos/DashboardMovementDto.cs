using System;
using StockPilot.Entity;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class DashboardMovementDto
	{
		public long Id { get; set; }

		public DateTime MovementDate { get; set; }

		public DateTime CreationTime { get; set; }

		public StockMovementType MovementType { get; set; }

		public int ItemMasterId { get; set; }

		public string ItemName { get; set; }

		public string BaseUnitCode { get; set; }

		public decimal Quantity { get; set; }

		public decimal BalanceAfter { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }

		public string ReferenceNo { get; set; }
	}
}
