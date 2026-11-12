using System;
using System.Collections.Generic;
using System.Linq;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public static class FifoAllocator
	{
		private class Lot
		{
			public FifoMovement Source { get; }

			public decimal Remaining { get; set; }

			public Lot(FifoMovement source, decimal quantity)
			{
				Source = source;
				Remaining = quantity;
			}
		}

		public static FifoResult Allocate(IEnumerable<FifoMovement> movements)
		{
			FifoResult result = new FifoResult();
			List<Lot> lots = new List<Lot>();
			Dictionary<long, FifoMovement> byId = new Dictionary<long, FifoMovement>();
			foreach (FifoMovement movement in movements.OrderBy((FifoMovement m) => m.Id))
			{
				byId[movement.Id] = movement;
				if (movement.Quantity > 0m)
				{
					decimal unmatched = ((movement.MovementType == StockMovementType.SaleReturnIn) ? ReverseSale(movement, result, lots, byId) : movement.Quantity);
					if (unmatched > 0m)
					{
						lots.Add(new Lot(movement, unmatched));
					}
				}
				else if (movement.Quantity < 0m)
				{
					decimal needed = -movement.Quantity;
					if (movement.MovementType == StockMovementType.PurchaseReturnOut)
					{
						needed = ReturnToSupplier(movement, needed, lots, result);
					}
					TakeOldestFirst(movement, needed, lots, result);
				}
			}
			foreach (Lot lot in lots)
			{
				result.RemainingByLot[lot.Source.Id] = lot.Remaining;
			}
			return result;
		}

		private static void TakeOldestFirst(FifoMovement movement, decimal needed, List<Lot> lots, FifoResult result)
		{
			foreach (Lot lot in lots)
			{
				if (needed <= 0m)
				{
					return;
				}
				if (!(lot.Remaining <= 0m))
				{
					decimal taken = Math.Min(lot.Remaining, needed);
					lot.Remaining -= taken;
					needed -= taken;
					result.Allocations.Add(new FifoAllocation
					{
						InMovementId = lot.Source.Id,
						OutMovementId = movement.Id,
						Quantity = taken
					});
				}
			}
			if (needed > 0m)
			{
				result.Allocations.Add(new FifoAllocation
				{
					InMovementId = null,
					OutMovementId = movement.Id,
					Quantity = needed
				});
			}
		}

		private static decimal ReturnToSupplier(FifoMovement movement, decimal needed, List<Lot> lots, FifoResult result)
		{
			foreach (Lot lot in lots.Where((Lot l) => l.Source.MovementType == StockMovementType.PurchaseIn && l.Source.ReferenceType == movement.ReferenceType && l.Source.ReferenceId == movement.ReferenceId && l.Source.ReferenceItemId == movement.ReferenceItemId))
			{
				decimal taken = Math.Min(lot.Remaining, needed);
				lot.Remaining -= taken;
				needed -= taken;
				result.ReturnedByLot.TryGetValue(lot.Source.Id, out var returned);
				result.ReturnedByLot[lot.Source.Id] = returned + taken;
				if (needed <= 0m)
				{
					break;
				}
			}
			return needed;
		}

		private static decimal ReverseSale(FifoMovement movement, FifoResult result, List<Lot> lots, Dictionary<long, FifoMovement> byId)
		{
			decimal toReturn = movement.Quantity;
			foreach (FifoAllocation allocation in result.Allocations.Where((FifoAllocation a) => byId[a.OutMovementId].Quantity < 0m && (byId[a.OutMovementId].MovementType == StockMovementType.SaleOut || byId[a.OutMovementId].MovementType == StockMovementType.IssueOut) && byId[a.OutMovementId].ReferenceType == movement.ReferenceType && byId[a.OutMovementId].ReferenceId == movement.ReferenceId && byId[a.OutMovementId].ReferenceItemId == movement.ReferenceItemId).Reverse().ToList())
			{
				if (toReturn <= 0m)
				{
					break;
				}
				decimal back = Math.Min(allocation.Quantity, toReturn);
				Lot lot = (allocation.InMovementId.HasValue ? lots.FirstOrDefault((Lot l) => l.Source.Id == allocation.InMovementId.Value) : null);
				if (lot != null)
				{
					lot.Remaining += back;
				}
				allocation.Quantity -= back;
				toReturn -= back;
				if (allocation.Quantity <= 0m)
				{
					result.Allocations.Remove(allocation);
				}
			}
			return toReturn;
		}
	}
}
