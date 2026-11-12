using System;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class ItemQuantityDto
	{
		public int ItemId { get; set; }

		public string ItemName { get; set; }

		public string ItemCode { get; set; }

		public Guid? PictureId { get; set; }

		public decimal Quantity { get; set; }

		public string BaseUnitCode { get; set; }
	}
}
