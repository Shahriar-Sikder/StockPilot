namespace StockPilot.Inventory
{
	public static class ItemStockRules
	{
		public const decimal FullLevelMultiple = 3m;

		public static ItemStockStatus GetStatus(bool isStockItem, decimal quantityOnHand, decimal reorderLevel)
		{
			if (!isStockItem)
			{
				return ItemStockStatus.NotStocked;
			}
			if (quantityOnHand <= 0m)
			{
				return ItemStockStatus.Out;
			}
			if (!(quantityOnHand <= reorderLevel))
			{
				return ItemStockStatus.Ok;
			}
			return ItemStockStatus.Low;
		}

		public static decimal GetLevelRatio(decimal quantityOnHand, decimal reorderLevel)
		{
			if (reorderLevel > 0m)
			{
				return quantityOnHand / (reorderLevel * 3m);
			}
			if (!(quantityOnHand > 0m))
			{
				return 0m;
			}
			return 1m;
		}
	}
}
