using System.Collections.Generic;

namespace StockPilot.Inventory
{
	public class FifoResult
	{
		public List<FifoAllocation> Allocations { get; } = new List<FifoAllocation>();

		public Dictionary<long, decimal> RemainingByLot { get; } = new Dictionary<long, decimal>();

		public Dictionary<long, decimal> ReturnedByLot { get; } = new Dictionary<long, decimal>();
	}
}
