using System;
using StockPilot.Dto;
using StockPilot.Entity;

namespace StockPilot.Services.StockMovement.Dtos
{
	public class StockMovementGridInput : PagedSortedAndFilteredInputDto
	{
		public DateTime? DateFrom { get; set; }

		public DateTime? DateTo { get; set; }

		public int? ItemMasterId { get; set; }

		public StockMovementType? MovementType { get; set; }

		public string ReferenceType { get; set; }

		public int? ReferenceId { get; set; }
	}
}
