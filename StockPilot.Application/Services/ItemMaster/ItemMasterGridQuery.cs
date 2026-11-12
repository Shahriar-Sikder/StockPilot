using System.Collections.Generic;
using System.Linq;
using StockPilot.Entity;

namespace StockPilot.Services.ItemMaster
{
	public static class ItemMasterGridQuery
	{
		public static IQueryable<ItemMasterGridRow> Build(IQueryable<StockPilot.Entity.ItemMaster> items, IQueryable<StockBalance> balances)
		{
			return from i in items
				join b in balances on i.Id equals b.ItemMasterId into itemBalances
				from b in itemBalances.DefaultIfEmpty()
				let quantity = ((decimal?)b.QuantityOnHand) ?? 0m
				let average = ((decimal?)b.AverageCost) ?? 0m
				select new ItemMasterGridRow
				{
					Id = i.Id,
					ItemCode = i.ItemCode,
					ItemName = i.ItemName,
					Barcode = i.Barcode,
					ItemCategoryId = i.ItemCategoryId,
					ItemCategoryName = i.ItemCategoryInfo.Name,
					ItemSubCategoryId = i.ItemSubCategoryId,
					ItemSubCategoryName = i.ItemSubCategoryInfo.Name,
					BaseUnitId = i.BaseUnitId,
					BaseUnitCode = i.BaseUnit.Code,
					PictureId = i.PictureId,
					SalePrice = i.SalePrice,
					ReorderLevel = i.ReorderLevel,
					QuantityOnHand = quantity,
					AverageCost = average,
					StockValue = quantity * average,
					StatusCode = ((!i.IsStockItem) ? 3 : ((quantity <= 0m) ? 2 : ((quantity <= i.ReorderLevel) ? 1 : 0))),
					LevelRatio = ((i.ReorderLevel > 0m) ? (quantity / (i.ReorderLevel * 3m)) : ((quantity > 0m) ? 1m : 0m)),
					IsStockItem = i.IsStockItem,
					IsActive = i.IsActive,
					IsDeleted = i.IsDeleted,
					CreationTime = i.CreationTime
				};
		}
	}
}
