using System.Collections.Generic;

namespace StockPilot.Inventory
{
	public class InventoryInsight
	{
		public InventoryInsightType Type { get; set; }

		public InsightSeverity Severity { get; set; }

		public int? ItemId { get; set; }

		public string ItemName { get; set; }

		public string MessageKey { get; set; }

		public List<string> MessageParams { get; set; } = new List<string>();

		public decimal? SuggestedQuantity { get; set; }

		public InsightActionType ActionType { get; set; }

		public string DocumentType { get; set; }

		public int? DocumentId { get; set; }
	}
}
