namespace StockPilot.Inventory
{
	public enum InventoryInsightType
	{
		OutOfStockWithDemand = 1,
		ReorderNow,
		LowCover,
		FastMover,
		SlowMoving,
		DeadStock,
		Overstock,
		BuyingMoreThanSelling,
		OldDrafts
	}
}
