using System;
using StockPilot.Inventory;

namespace StockPilot.Services.Dashboard.Dtos
{
	public class DashboardAttentionItemDto
	{
		public DashboardAttentionKind Kind { get; set; }

		public int Id { get; set; }

		public string DocumentNo { get; set; }

		public DateTime? DocumentDate { get; set; }

		public string PartyName { get; set; }

		public decimal? TotalAmount { get; set; }

		public string CurrencyCode { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string PictureUrl { get; set; }

		public decimal? QuantityOnHand { get; set; }

		public decimal? ReorderLevel { get; set; }

		public string BaseUnitCode { get; set; }

		public ItemStockStatus? Status { get; set; }
	}
}
