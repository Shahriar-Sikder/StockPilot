namespace StockPilot.Inventory
{
	public static class InventoryInsightConsts
	{
		public const int PeriodDays = 14;

		public const int TopItemCount = 5;

		public const int AnalysisDays = 30;

		public const int DeadStockDays = 60;

		public const int LowCoverDays = 7;

		public const int OverstockCoverDays = 90;

		public const decimal FastMoverTopShare = 0.10m;

		public const int FastMoverMinMovements = 5;

		public const decimal BuyingMoreThanSellingRatio = 2m;

		public const int OldDraftDays = 3;

		public const decimal ReorderLevelMultiple = 2m;

		public const int MaxInsights = 8;
	}
}
