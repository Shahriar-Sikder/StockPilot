using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class InventoryInsightManager : StockPilotDomainServiceBase
	{
		private readonly IRepository<ItemMaster> _itemRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly IRepository<StockMovement, long> _movementRepository;

		private readonly IRepository<PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<IssueSale> _issueSaleRepository;

		public InventoryInsightManager(IRepository<ItemMaster> itemRepository, IRepository<StockBalance> balanceRepository, IRepository<StockMovement, long> movementRepository, IRepository<PurchaseInvoice> purchaseInvoiceRepository, IRepository<IssueSale> issueSaleRepository)
		{
			_itemRepository = itemRepository;
			_balanceRepository = balanceRepository;
			_movementRepository = movementRepository;
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_issueSaleRepository = issueSaleRepository;
		}

		public virtual IQueryable<StockMovement> GetEffectiveMovements()
		{
			IQueryable<int> cancelledPurchases = from d in _purchaseInvoiceRepository.GetAll()
				where (int)d.Status == 2
				select d.Id;
			IQueryable<int> cancelledSales = from d in _issueSaleRepository.GetAll()
				where (int)d.Status == 2
				select d.Id;
			return from m in _movementRepository.GetAll()
				where !(m.ReferenceType == "PurchaseInvoice" && m.ReferenceId != (int?)null && cancelledPurchases.Contains(m.ReferenceId.Value)) && !(m.ReferenceType == "IssueSale" && m.ReferenceId != (int?)null && cancelledSales.Contains(m.ReferenceId.Value))
				select m;
		}

		public virtual Task<List<InventoryInsight>> GetInsightsAsync(DateTime now)
		{
			DateTime today = now.Date;
			DateTime analysisFrom = today.AddDays(-29.0);
			DateTime deadFrom = today.AddDays(-59.0);
			List<InsightItemData> items = (from i in _itemRepository.GetAll()
				where i.IsStockItem && i.IsActive
				select new InsightItemData
				{
					ItemId = i.Id,
					ItemName = i.ItemName,
					BaseUnitCode = i.BaseUnit.Code,
					ReorderLevel = i.ReorderLevel,
					MaxStock = i.MaxStock
				}).ToList();
			var onHand = (from b in _balanceRepository.GetAll()
				group b by b.ItemMasterId into g
				select new
				{
					ItemId = g.Key,
					Quantity = g.Sum((StockBalance b) => b.QuantityOnHand)
				}).ToList();
			var firstMovements = (from m in _movementRepository.GetAll()
				group m by m.ItemMasterId into g
				select new
				{
					ItemId = g.Key,
					Date = g.Min((StockMovement m) => m.MovementDate)
				}).ToList();
			var recent = (from m in GetEffectiveMovements()
				where m.MovementDate >= deadFrom && ((int)m.MovementType == 4 || (int)m.MovementType == 6 || (int)m.MovementType == 2)
				select new { m.ItemMasterId, m.MovementType, m.MovementDate, m.Quantity }).ToList();
			foreach (InsightItemData item in items)
			{
				int id = item.ItemId;
				item.OnHand = onHand.Where(b => b.ItemId == id).Sum(b => b.Quantity);
				item.FirstMovementDate = (from f in firstMovements
					where f.ItemId == id
					select (DateTime?)f.Date).FirstOrDefault();
				var ofItem = recent.Where(m => m.ItemMasterId == id).ToList();
				var outs = ofItem.Where(m => m.MovementType != StockMovementType.PurchaseIn).ToList();
				var analysisOuts = outs.Where(m => m.MovementDate >= analysisFrom).ToList();
				item.OutQuantity = analysisOuts.Sum(m => Math.Abs(m.Quantity));
				item.OutMovementCount = analysisOuts.Count;
				item.InQuantity = ofItem.Where(m => m.MovementType == StockMovementType.PurchaseIn && m.MovementDate >= analysisFrom).Sum(m => m.Quantity);
				item.LastOutDate = outs.Select(m => (DateTime?)m.MovementDate).Max();
			}
			DateTime draftsBefore = now.AddDays(-3.0);
			List<InsightDraftData> drafts = (from d in _purchaseInvoiceRepository.GetAll()
				where (int)d.Status == 0 && d.CreationTime < draftsBefore
				select new InsightDraftData
				{
					DocumentType = "PurchaseInvoice",
					DocumentId = d.Id,
					DocumentNo = d.InvoiceNo,
					CreationTime = d.CreationTime
				}).ToList();
			drafts.AddRange((from d in _issueSaleRepository.GetAll()
				where (int)d.Status == 0 && d.CreationTime < draftsBefore
				select new InsightDraftData
				{
					DocumentType = "IssueSale",
					DocumentId = d.Id,
					DocumentNo = d.DocumentNo,
					CreationTime = d.CreationTime
				}).ToList());
			return Task.FromResult(BuildInsights(items, drafts, now));
		}

		public static List<InventoryInsight> BuildInsights(IList<InsightItemData> items, IList<InsightDraftData> drafts, DateTime now)
		{
			items = items ?? new List<InsightItemData>();
			drafts = drafts ?? new List<InsightDraftData>();
			DateTime today = now.Date;
			DateTime analysisFrom = today.AddDays(-29.0);
			DateTime deadFrom = today.AddDays(-59.0);
			ISet<int> fastMovers = FastMoverIds(items);
			List<InventoryInsight> insights = new List<InventoryInsight>();
			foreach (InsightItemData item in items)
			{
				InventoryInsight best = (from c in Candidates(item, fastMovers, analysisFrom, deadFrom).Select((InventoryInsight insight, int order) => new { insight, order })
					orderby c.insight.Severity, c.order
					select c.insight).FirstOrDefault();
				if (best != null)
				{
					best.ItemId = item.ItemId;
					best.ItemName = item.ItemName;
					insights.Add(best);
				}
			}
			List<InsightDraftData> oldDrafts = (from d in drafts
				where d.CreationTime < now.AddDays(-3.0)
				orderby d.CreationTime, d.DocumentId
				select d).ToList();
			if (oldDrafts.Count > 0)
			{
				InsightDraftData oldest = oldDrafts[0];
				insights.Add(new InventoryInsight
				{
					Type = InventoryInsightType.OldDrafts,
					Severity = InsightSeverity.Warning,
					MessageKey = MessageKey(InventoryInsightType.OldDrafts),
					MessageParams = 
					{
						oldDrafts.Count.ToString(CultureInfo.InvariantCulture),
						oldest.DocumentNo,
						((int)(now - oldest.CreationTime).TotalDays).ToString(CultureInfo.InvariantCulture)
					},
					ActionType = InsightActionType.ReviewDraft,
					DocumentType = oldest.DocumentType,
					DocumentId = oldest.DocumentId
				});
			}
			return insights.OrderBy((InventoryInsight i) => i.Severity).ThenBy((InventoryInsight i) => i.ItemName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase).Take(8)
				.ToList();
		}

		public static decimal SuggestReorderQuantity(InsightItemData item)
		{
			decimal target = Math.Max(item.MaxStock, item.ReorderLevel * 2m);
			return Math.Max(1m, Math.Ceiling(target - item.OnHand));
		}

		public static string MessageKey(InventoryInsightType type)
		{
			return "Insight_" + type;
		}

		private static IEnumerable<InventoryInsight> Candidates(InsightItemData item, ISet<int> fastMovers, DateTime analysisFrom, DateTime deadFrom)
		{
			string unit = item.BaseUnitCode;
			decimal averageDailyOut = item.OutQuantity / 30m;
			decimal? cover = ((averageDailyOut > 0m) ? new decimal?(item.OnHand / averageDailyOut) : ((decimal?)null));
			if (item.OnHand <= 0m && item.OutQuantity > 0m)
			{
				yield return Reorder(InventoryInsightType.OutOfStockWithDemand, InsightSeverity.Critical, item, Num(item.OutQuantity), unit);
			}
			if (item.ReorderLevel > 0m && item.OnHand <= item.ReorderLevel)
			{
				yield return Reorder(InventoryInsightType.ReorderNow, InsightSeverity.Warning, item, Num(item.OnHand), unit, Num(item.ReorderLevel));
			}
			if (cover.HasValue && cover.Value < 7m)
			{
				yield return Reorder(InventoryInsightType.LowCover, InsightSeverity.Warning, item, Num(Math.Max(0m, Math.Floor(cover.Value))), Num(averageDailyOut), unit);
			}
			bool stockedSinceDead = item.FirstMovementDate.HasValue && item.FirstMovementDate.Value.Date < deadFrom;
			if (item.OnHand > 0m && stockedSinceDead && (!item.LastOutDate.HasValue || item.LastOutDate.Value.Date < deadFrom))
			{
				yield return Note(InventoryInsightType.DeadStock, InsightSeverity.Warning, Num(item.OnHand), unit, Num(60));
			}
			if (fastMovers.Contains(item.ItemId))
			{
				yield return Note(InventoryInsightType.FastMover, InsightSeverity.Info, Num(item.OutQuantity), unit, Num(item.OutMovementCount));
			}
			bool stockedSinceAnalysis = item.FirstMovementDate.HasValue && item.FirstMovementDate.Value.Date < analysisFrom;
			if (item.OnHand > 0m && stockedSinceAnalysis && (!item.LastOutDate.HasValue || item.LastOutDate.Value.Date < analysisFrom))
			{
				yield return Note(InventoryInsightType.SlowMoving, InsightSeverity.Info, Num(item.OnHand), unit, Num(30));
			}
			if (item.MaxStock > 0m && item.OnHand > item.MaxStock)
			{
				yield return Note(InventoryInsightType.Overstock, InsightSeverity.Info, Num(item.OnHand), unit, Num(item.MaxStock));
			}
			else if (cover.HasValue && cover.Value > 90m)
			{
				InventoryInsight insight = Note(InventoryInsightType.Overstock, InsightSeverity.Info, Num(item.OnHand), unit, Num(Math.Floor(cover.Value)));
				insight.MessageKey = MessageKey(InventoryInsightType.Overstock) + "Cover";
				yield return insight;
			}
			if (item.OutQuantity > 0m && item.InQuantity > 2m * item.OutQuantity)
			{
				yield return Note(InventoryInsightType.BuyingMoreThanSelling, InsightSeverity.Info, Num(item.InQuantity), Num(item.OutQuantity), unit);
			}
		}

		private static ISet<int> FastMoverIds(IList<InsightItemData> items)
		{
			int take = Math.Max(1, (int)Math.Ceiling((decimal)items.Count * 0.10m));
			return new HashSet<int>(from i in (from i in items
					where i.OutQuantity > 0m
					orderby i.OutQuantity descending, i.ItemId
					select i).Take(take)
				where i.OutMovementCount >= 5
				select i.ItemId);
		}

		private static InventoryInsight Reorder(InventoryInsightType type, InsightSeverity severity, InsightItemData item, params string[] messageParams)
		{
			InventoryInsight inventoryInsight = Note(type, severity, messageParams);
			inventoryInsight.ActionType = InsightActionType.Reorder;
			inventoryInsight.SuggestedQuantity = SuggestReorderQuantity(item);
			return inventoryInsight;
		}

		private static InventoryInsight Note(InventoryInsightType type, InsightSeverity severity, params string[] messageParams)
		{
			return new InventoryInsight
			{
				Type = type,
				Severity = severity,
				MessageKey = MessageKey(type),
				MessageParams = messageParams.ToList(),
				ActionType = InsightActionType.None
			};
		}

		private static string Num(decimal value)
		{
			return Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
		}

		private static string Num(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
