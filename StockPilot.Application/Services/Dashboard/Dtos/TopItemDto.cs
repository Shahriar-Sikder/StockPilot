using System;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class TopItemDto
	{
		public int ItemId { get; set; }

		public string ItemName { get; set; }

		public string ItemCode { get; set; }

		public string PictureUrl { get; set; }

		public decimal Quantity { get; set; }

		public string BaseUnitCode { get; set; }

		public int DocumentCount { get; set; }
	}
}
