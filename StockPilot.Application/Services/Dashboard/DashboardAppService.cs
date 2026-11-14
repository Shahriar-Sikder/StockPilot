using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Timing;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Dashboard.Dtos;
using StockPilot.Services.ItemMaster;

namespace StockPilot.Services.Dashboard
{
	[AbpAuthorize(new string[] { })]
	public class DashboardAppService : StockPilotAppServiceBase, IDashboardAppService, IApplicationService, ITransientDependency
	{
		public const int PeriodDays = 14;

		public const int RecentMovementCount = 6;

		public const int AttentionRowsPerKind = 10;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly IRepository<StockPilot.Entity.StockMovement, long> _movementRepository;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<StockPilot.Entity.IssueSale> _issueSaleRepository;

		private readonly InventoryInsightManager _insightManager;

		public DashboardAppService(IRepository<StockPilot.Entity.ItemMaster> itemRepository, IRepository<StockBalance> balanceRepository, IRepository<StockPilot.Entity.StockMovement, long> movementRepository, IRepository<StockPilot.Entity.PurchaseInvoice> purchaseInvoiceRepository, IRepository<StockPilot.Entity.IssueSale> issueSaleRepository, InventoryInsightManager insightManager)
		{
			_itemRepository = itemRepository;
			_balanceRepository = balanceRepository;
			_movementRepository = movementRepository;
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_issueSaleRepository = issueSaleRepository;
			_insightManager = insightManager;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot" })]
		public async Task<DashboardSummaryDto> GetSummary()
		{
			DashboardSummaryDto summary = new DashboardSummaryDto();
			await FillStockAsync(summary);
			await FillPeriodAsync(summary);
			await FillDraftsAsync(summary);
			await FillLowStockAsync(summary);
			await FillRecentMovementsAsync(summary);
			return summary;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot" })]
		public async Task<QuantityDashboardDto> GetQuantitySummary()
		{
			DateTime now = Clock.Now;
			DateTime today = now.Date;
			QuantityDashboardDto summary = new QuantityDashboardDto
			{
				PeriodFrom = today.AddDays(-13.0),
				PeriodTo = today,
				MonthFrom = new DateTime(today.Year, today.Month, 1)
			};
			DateTime periodTo = today.AddDays(1.0);
			QuantityDashboardDto quantityDashboardDto = summary;
			quantityDashboardDto.TotalItems = await _itemRepository.GetAll().CountAsync((StockPilot.Entity.ItemMaster i) => i.IsStockItem && i.IsActive);
			IQueryable<StockPilot.Entity.StockMovement> effective = _insightManager.GetEffectiveMovements();
			IQueryable<StockPilot.Entity.StockMovement> inPeriod = effective.Where((StockPilot.Entity.StockMovement m) => m.MovementDate >= summary.PeriodFrom && m.MovementDate < periodTo);
			quantityDashboardDto = summary;
			quantityDashboardDto.MostIn = (await TopItemsAsync(inPeriod.Where((StockPilot.Entity.StockMovement m) => (int)m.MovementType == 2), 1)).Select(ToItemQuantity).FirstOrDefault();
			quantityDashboardDto = summary;
			quantityDashboardDto.MostOut = (await TopItemsAsync(inPeriod.Where((StockPilot.Entity.StockMovement m) => (int)m.MovementType == 4 || (int)m.MovementType == 6), 1)).Select(ToItemQuantity).FirstOrDefault();
			IQueryable<StockPilot.Entity.StockMovement> inMonth = effective.Where((StockPilot.Entity.StockMovement m) => m.MovementDate >= summary.MonthFrom && m.MovementDate < periodTo);
			quantityDashboardDto = summary;
			quantityDashboardDto.TopReceivedThisMonth = await TopItemsAsync(inMonth.Where((StockPilot.Entity.StockMovement m) => (int)m.MovementType == 2), 5);
			quantityDashboardDto = summary;
			quantityDashboardDto.TopSoldThisMonth = await TopItemsAsync(inMonth.Where((StockPilot.Entity.StockMovement m) => (int)m.MovementType == 4), 5);
			quantityDashboardDto = summary;
			quantityDashboardDto.InOutChart = await GetDailyMovementCountsAsync(summary.PeriodFrom, today);
			quantityDashboardDto = summary;
			quantityDashboardDto.StockHealth = await GetHealthAsync();
			summary.LowStockCount = summary.StockHealth.LowCount + summary.StockHealth.OutCount;
			quantityDashboardDto = summary;
			quantityDashboardDto.DraftCount = await CountDraftsAsync();
			quantityDashboardDto = summary;
			quantityDashboardDto.Attention = (await GetDraftAttentionAsync()).Concat(await GetLowStockAttentionAsync()).Select(ToAttentionItem).ToList();
			quantityDashboardDto = summary;
			quantityDashboardDto.RecentActivity = await GetRecentMovementsAsync();
			quantityDashboardDto = summary;
			quantityDashboardDto.Insights = (await _insightManager.GetInsightsAsync(now)).Select(ToInsightDto).ToList();
			return summary;
		}

		private async Task<List<TopItemDto>> TopItemsAsync(IQueryable<StockPilot.Entity.StockMovement> movements, int count)
		{
			var top = await (from m in movements
				group m by m.ItemMasterId into g
				select new
				{
					ItemId = g.Key,
					Quantity = g.Sum((StockPilot.Entity.StockMovement m) => (m.Quantity < 0m) ? (-m.Quantity) : m.Quantity),
					DocumentCount = (from m in g
						where m.ReferenceId != (int?)null
						select m.ReferenceId).Distinct().Count()
				} into t
				where t.Quantity > 0m
				orderby t.Quantity descending, t.ItemId
				select t).Take(count).ToListAsync();
			List<int> ids = top.Select(t => t.ItemId).ToList();
			var items = await (from i in _itemRepository.GetAll()
				where ids.Contains(i.Id)
				select new
				{
					Id = i.Id,
					ItemName = i.ItemName,
					ItemCode = i.ItemCode,
					PictureUrl = i.PictureUrl,
					BaseUnitCode = i.BaseUnit.Code
				}).ToListAsync();
			return top.Select(t =>
			{
				var anon = items.Single(i => i.Id == t.ItemId);
				return new TopItemDto
				{
					ItemId = t.ItemId,
					ItemName = anon.ItemName,
					ItemCode = anon.ItemCode,
					PictureUrl = anon.PictureUrl,
					BaseUnitCode = anon.BaseUnitCode,
					Quantity = t.Quantity,
					DocumentCount = t.DocumentCount
				};
			}).ToList();
		}

		private async Task<List<DailyMovementCountDto>> GetDailyMovementCountsAsync(DateTime from, DateTime today)
		{
			DateTime toExclusive = today.AddDays(1.0);
			var movements = await (from m in _movementRepository.GetAll()
				where m.MovementDate >= @from && m.MovementDate < toExclusive
				select new { m.MovementDate, m.Quantity, m.MovementType }).ToListAsync();
			List<DailyMovementCountDto> days = new List<DailyMovementCountDto>();
			DateTime day = from;
			while (day <= today)
			{
				DateTime current = day;
				var ofDay = movements.Where(m => m.MovementDate.Date == current).ToList();
				days.Add(new DailyMovementCountDto
				{
					Date = current,
					InCount = ofDay.Count(m => m.Quantity > 0m && m.MovementType != StockMovementType.Opening),
					OutCount = ofDay.Count(m => m.Quantity < 0m)
				});
				day = day.AddDays(1.0);
			}
			return days;
		}

		private static ItemQuantityDto ToItemQuantity(TopItemDto top)
		{
			return new ItemQuantityDto
			{
				ItemId = top.ItemId,
				ItemName = top.ItemName,
				ItemCode = top.ItemCode,
				PictureUrl = top.PictureUrl,
				Quantity = top.Quantity,
				BaseUnitCode = top.BaseUnitCode
			};
		}

		private static AttentionItemDto ToAttentionItem(DashboardAttentionItemDto row)
		{
			return new AttentionItemDto
			{
				Kind = row.Kind,
				Id = row.Id,
				DocumentNo = row.DocumentNo,
				DocumentDate = row.DocumentDate,
				PartyName = row.PartyName,
				ItemCode = row.ItemCode,
				ItemName = row.ItemName,
				PictureUrl = row.PictureUrl,
				QuantityOnHand = row.QuantityOnHand,
				ReorderLevel = row.ReorderLevel,
				BaseUnitCode = row.BaseUnitCode,
				Status = row.Status
			};
		}

		private static InsightDto ToInsightDto(InventoryInsight insight)
		{
			return new InsightDto
			{
				Type = insight.Type,
				Severity = insight.Severity,
				ItemId = insight.ItemId,
				ItemName = insight.ItemName,
				MessageKey = insight.MessageKey,
				MessageParams = insight.MessageParams,
				SuggestedQuantity = insight.SuggestedQuantity,
				ActionType = insight.ActionType,
				DocumentType = insight.DocumentType,
				DocumentId = insight.DocumentId
			};
		}

		private IQueryable<ItemMasterGridRow> StockItems()
		{
			return from r in ItemMasterGridQuery.Build(_itemRepository.GetAll(), _balanceRepository.GetAll())
				where r.IsStockItem
				select r;
		}

		private async Task FillStockAsync(DashboardSummaryDto summary)
		{
			DashboardSummaryDto dashboardSummaryDto = summary;
			dashboardSummaryDto.StockValue = StockManager.Round((await StockItems().SumAsync((Expression<Func<ItemMasterGridRow, decimal?>>)((ItemMasterGridRow r) => r.StockValue))).GetValueOrDefault());
			dashboardSummaryDto = summary;
			dashboardSummaryDto.Health = await GetHealthAsync();
			summary.TrackedItemCount = summary.Health.TotalCount;
			summary.LowStockCount = summary.Health.LowCount + summary.Health.OutCount;
		}

		private async Task<DashboardHealthDto> GetHealthAsync()
		{
			var counts = await (from r in StockItems()
				group r by r.StatusCode into g
				select new
				{
					StatusCode = g.Key,
					Count = g.Count()
				}).ToListAsync();
			return new DashboardHealthDto
			{
				OkCount = counts.Where(c => c.StatusCode == 0).Sum(c => c.Count),
				LowCount = counts.Where(c => c.StatusCode == 1).Sum(c => c.Count),
				OutCount = counts.Where(c => c.StatusCode == 2).Sum(c => c.Count)
			};
		}

		private async Task FillPeriodAsync(DashboardSummaryDto summary)
		{
			DateTime today = Clock.Now.Date;
			summary.PeriodFrom = today.AddDays(-13.0);
			summary.PeriodTo = today;
			DateTime from = summary.PeriodFrom;
			DateTime toExclusive = today.AddDays(1.0);
			var movements = await (from m in _movementRepository.GetAll()
				where m.MovementDate >= @from && m.MovementDate < toExclusive
				select new { m.MovementDate, m.Quantity, m.TotalValue, m.MovementType }).ToListAsync();
			DateTime day = from;
			while (day <= today)
			{
				DateTime current = day;
				var ofDay = movements.Where(m => m.MovementDate.Date == current).ToList();
				summary.Days.Add(new DashboardDayDto
				{
					Date = current,
					StockIn = ofDay.Where(m => m.Quantity > 0m && m.MovementType != StockMovementType.Opening).Sum(m => m.TotalValue),
					StockOut = -ofDay.Where(m => m.Quantity < 0m).Sum(m => m.TotalValue)
				});
				day = day.AddDays(1.0);
			}
			summary.StockInTotal = summary.Days.Sum((DashboardDayDto d) => d.StockIn);
			summary.StockOutTotal = summary.Days.Sum((DashboardDayDto d) => d.StockOut);
		}

		private async Task FillDraftsAsync(DashboardSummaryDto summary)
		{
			summary.DraftCount = await CountDraftsAsync();
			List<DashboardAttentionItemDto> attention = summary.Attention;
			attention.AddRange(await GetDraftAttentionAsync());
		}

		private async Task<int> CountDraftsAsync()
		{
			return await _purchaseInvoiceRepository.GetAll().CountAsync((StockPilot.Entity.PurchaseInvoice d) => (int)d.Status == 0) + await _issueSaleRepository.GetAll().CountAsync((StockPilot.Entity.IssueSale d) => (int)d.Status == 0);
		}

		private async Task<List<DashboardAttentionItemDto>> GetDraftAttentionAsync()
		{
			IQueryable<StockPilot.Entity.PurchaseInvoice> purchaseDrafts = from d in _purchaseInvoiceRepository.GetAll()
				where (int)d.Status == 0
				select d;
			IQueryable<StockPilot.Entity.IssueSale> saleDrafts = from d in _issueSaleRepository.GetAll()
				where (int)d.Status == 0
				select d;
			List<DashboardAttentionItemDto> rows = new List<DashboardAttentionItemDto>();
			List<DashboardAttentionItemDto> list = rows;
			list.AddRange(await (from d in (from d in purchaseDrafts
					orderby d.InvoiceDate descending, d.Id descending
					select d).Take(10)
				select new DashboardAttentionItemDto
				{
					Kind = DashboardAttentionKind.PurchaseDraft,
					Id = d.Id,
					DocumentNo = d.InvoiceNo,
					DocumentDate = d.InvoiceDate,
					PartyName = d.SupplierInfo.Name,
					TotalAmount = d.TotalAmount,
					CurrencyCode = d.CurrencyInfo.Code
				}).ToListAsync());
			list = rows;
			list.AddRange(await (from d in (from d in saleDrafts
					orderby d.DocumentDate descending, d.Id descending
					select d).Take(10)
				select new DashboardAttentionItemDto
				{
					Kind = DashboardAttentionKind.SaleDraft,
					Id = d.Id,
					DocumentNo = d.DocumentNo,
					DocumentDate = d.DocumentDate,
					PartyName = d.CustomerName,
					TotalAmount = d.TotalAmount,
					CurrencyCode = d.CurrencyInfo.Code
				}).ToListAsync());
			return rows;
		}

		private async Task FillLowStockAsync(DashboardSummaryDto summary)
		{
			List<DashboardAttentionItemDto> attention = summary.Attention;
			attention.AddRange(await GetLowStockAttentionAsync());
		}

		private async Task<List<DashboardAttentionItemDto>> GetLowStockAttentionAsync()
		{
			return (await (from r in StockItems()
				where r.StatusCode == 1 || r.StatusCode == 2
				orderby r.StatusCode descending, r.LevelRatio, r.ItemName
				select r).Take(10).ToListAsync()).Select((ItemMasterGridRow r) => new DashboardAttentionItemDto
			{
				Kind = DashboardAttentionKind.LowStock,
				Id = r.Id,
				ItemCode = r.ItemCode,
				ItemName = r.ItemName,
				PictureUrl = r.PictureUrl,
				QuantityOnHand = r.QuantityOnHand,
				ReorderLevel = r.ReorderLevel,
				BaseUnitCode = r.BaseUnitCode,
				Status = (ItemStockStatus)r.StatusCode
			}).ToList();
		}

		private async Task FillRecentMovementsAsync(DashboardSummaryDto summary)
		{
			summary.RecentMovements = await GetRecentMovementsAsync();
		}

		private async Task<List<DashboardMovementDto>> GetRecentMovementsAsync()
		{
			return await (from m in (from m in _movementRepository.GetAll()
					orderby m.Id descending
					select m).Take(6)
				select new DashboardMovementDto
				{
					Id = m.Id,
					MovementDate = m.MovementDate,
					CreationTime = m.CreationTime,
					MovementType = m.MovementType,
					ItemMasterId = m.ItemMasterId,
					ItemName = m.ItemMasterInfo.ItemName,
					BaseUnitCode = m.ItemMasterInfo.BaseUnit.Code,
					Quantity = m.Quantity,
					BalanceAfter = m.BalanceAfter,
					ReferenceType = m.ReferenceType,
					ReferenceId = m.ReferenceId,
					ReferenceNo = m.ReferenceNo
				}).ToListAsync();
		}
	}
}
