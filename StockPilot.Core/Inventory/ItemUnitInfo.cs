namespace StockPilot.Inventory
{
	public class ItemUnitInfo
	{
		public int UnitId { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }

		public decimal Factor { get; set; }

		public bool IsBaseUnit { get; set; }

		public bool IsActive { get; set; }
	}
}
