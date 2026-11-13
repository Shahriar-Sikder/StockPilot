using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.InventoryReport.Dtos;
using StockPilot.Services.InventoryReport.Exporting;
using StockPilot.Services.ItemMaster;

namespace StockPilot.Services.InventoryReport
{
	[AbpAuthorize(new string[] { })]
	[RequiresFeature(new string[] { "App.Inventory.Reports" })]
	public class InventoryReportAppService : MasterDataAppServiceBase, IInventoryReportAppService, IApplicationService, ITransientDependency
	{
		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly IRepository<StockPilot.Entity.StockMovement, long> _movementRepository;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _invoiceRepository;

		private readonly IRepository<StockPilot.Entity.IssueSale> _issueSaleRepository;

		private readonly InventoryReportExcelExporter _exporter;

		public InventoryReportAppService(IRepository<StockPilot.Entity.ItemMaster> itemRepository, IRepository<StockBalance> balanceRepository, IRepository<StockPilot.Entity.StockMovement, long> movementRepository, IRepository<StockPilot.Entity.PurchaseInvoice> invoiceRepository, IRepository<StockPilot.Entity.IssueSale> issueSaleRepository, InventoryReportExcelExporter exporter)
		{
			_itemRepository = itemRepository;
			_balanceRepository = balanceRepository;
			_movementRepository = movementRepository;
			_invoiceRepository = invoiceRepository;
			_issueSaleRepository = issueSaleRepository;
			_exporter = exporter;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		public async Task<StockItemListReportDto> GetStockItemList(StockItemListInput input)
		{
			int? statusCode = (int?)input.Status;
			List<ItemMasterGridRow> rows = await (from r in (from r in ItemMasterGridQuery.Build(_itemRepository.GetAll(), _balanceRepository.GetAll())
					where r.IsStockItem
					select r).WhereIf(input.ItemCategoryId.HasValue, (ItemMasterGridRow r) => r.ItemCategoryId == input.ItemCategoryId.Value).WhereIf(statusCode.HasValue, (ItemMasterGridRow r) => r.StatusCode == ((int?)statusCode).Value).WhereIf(!input.IncludeInactive, (ItemMasterGridRow r) => r.IsActive)
					.WhereIf(!input.Filter.IsNullOrWhiteSpace(), (ItemMasterGridRow r) => r.ItemCode.Contains(input.Filter) || r.ItemName.Contains(input.Filter))
				orderby r.ItemName
				select r).Take(100000).ToListAsync();
			StockItemListReportDto obj = new StockItemListReportDto
			{
				Rows = rows.Select((ItemMasterGridRow r) => new StockItemListRowDto
				{
					ItemId = r.Id,
					ItemCode = r.ItemCode,
					ItemName = r.ItemName,
					ItemCategoryName = r.ItemCategoryName,
					ItemSubCategoryName = r.ItemSubCategoryName,
					BaseUnitCode = r.BaseUnitCode,
					QuantityOnHand = r.QuantityOnHand,
					AverageCost = r.AverageCost,
					StockValue = StockManager.Round(r.StockValue),
					ReorderLevel = r.ReorderLevel,
					Status = (ItemStockStatus)r.StatusCode
				}).ToList()
			};
			obj.ItemCount = obj.Rows.Count;
			obj.TotalValue = obj.Rows.Sum((StockItemListRowDto r) => r.StockValue);
			return obj;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetStockItemListToExcel(StockItemListInput input)
		{
			InventoryReportExcelExporter exporter = _exporter;
			return exporter.ExportStockItemList(await GetStockItemList(input));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		public async Task<ItemLedgerReportDto> GetItemLedger(ItemLedgerInput input)
		{
			StockPilot.Entity.ItemMaster item = await _itemRepository.GetAll().Include((StockPilot.Entity.ItemMaster i) => i.BaseUnit).FirstOrDefaultAsync((StockPilot.Entity.ItemMaster i) => i.Id == input.ItemMasterId);
			if (item == null)
			{
				throw new EntityNotFoundException(typeof(StockPilot.Entity.ItemMaster), input.ItemMasterId);
			}
			DateTime? from = input.DateFrom?.Date;
			DateTime? toExclusive = input.DateTo?.Date.AddDays(1.0);
			int itemId = item.Id;
			ItemLedgerReportDto report = new ItemLedgerReportDto
			{
				ItemId = item.Id,
				ItemCode = item.ItemCode,
				ItemName = item.ItemName,
				BaseUnitCode = item.BaseUnit?.Code,
				DateFrom = from,
				DateTo = input.DateTo?.Date
			};
			if (from.HasValue)
			{
				IQueryable<StockPilot.Entity.StockMovement> before = from stockMovement in _movementRepository.GetAll()
					where stockMovement.ItemMasterId == itemId && stockMovement.MovementDate < ((DateTime?)@from).Value
					select stockMovement;
				ItemLedgerReportDto itemLedgerReportDto = report;
				itemLedgerReportDto.OpeningQuantity = (await before.SumAsync((Expression<Func<StockPilot.Entity.StockMovement, decimal?>>)((StockPilot.Entity.StockMovement stockMovement) => stockMovement.Quantity))).GetValueOrDefault();
				itemLedgerReportDto = report;
				itemLedgerReportDto.OpeningValue = (await before.SumAsync((Expression<Func<StockPilot.Entity.StockMovement, decimal?>>)((StockPilot.Entity.StockMovement stockMovement) => stockMovement.TotalValue))).GetValueOrDefault();
			}
			List<StockPilot.Entity.StockMovement> obj = await (from stockMovement in (from stockMovement in _movementRepository.GetAll()
					where stockMovement.ItemMasterId == itemId
					select stockMovement).WhereIf(@from.HasValue, (StockPilot.Entity.StockMovement stockMovement) => stockMovement.MovementDate >= ((DateTime?)@from).Value).WhereIf(toExclusive.HasValue, (StockPilot.Entity.StockMovement stockMovement) => stockMovement.MovementDate < ((DateTime?)toExclusive).Value)
				orderby stockMovement.MovementDate, stockMovement.Id
				select stockMovement).Take(100000).ToListAsync();
			decimal balance = report.OpeningQuantity;
			foreach (StockPilot.Entity.StockMovement m in obj)
			{
				balance += m.Quantity;
				report.Rows.Add(new ItemLedgerRowDto
				{
					MovementId = m.Id,
					MovementDate = m.MovementDate,
					MovementType = m.MovementType,
					ReferenceType = m.ReferenceType,
					ReferenceId = m.ReferenceId,
					ReferenceNo = m.ReferenceNo,
					QuantityIn = ((m.Quantity > 0m) ? m.Quantity : 0m),
					QuantityOut = ((m.Quantity < 0m) ? (-m.Quantity) : 0m),
					UnitCost = m.UnitCost,
					Value = m.TotalValue,
					RunningBalance = balance,
					Remarks = m.Remarks
				});
			}
			report.TotalIn = report.Rows.Sum((ItemLedgerRowDto r) => r.QuantityIn);
			report.TotalOut = report.Rows.Sum((ItemLedgerRowDto r) => r.QuantityOut);
			report.TotalInValue = report.Rows.Where((ItemLedgerRowDto r) => r.Value > 0m).Sum((ItemLedgerRowDto r) => r.Value);
			report.TotalOutValue = -report.Rows.Where((ItemLedgerRowDto r) => r.Value < 0m).Sum((ItemLedgerRowDto r) => r.Value);
			report.ClosingQuantity = report.OpeningQuantity + report.TotalIn - report.TotalOut;
			report.ClosingValue = report.OpeningValue + report.TotalInValue - report.TotalOutValue;
			return report;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetItemLedgerToExcel(ItemLedgerInput input)
		{
			InventoryReportExcelExporter exporter = _exporter;
			return exporter.ExportItemLedger(await GetItemLedger(input));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		public async Task<InvoiceListReportDto> GetInvoiceList(InvoiceListInput input)
		{
			DateTime? from = input.DateFrom?.Date;
			DateTime? toExclusive = input.DateTo?.Date.AddDays(1.0);
			List<InvoiceListRowDto> rows = await (from d in (from d in _invoiceRepository.GetAll().WhereIf(@from.HasValue, (StockPilot.Entity.PurchaseInvoice d) => d.InvoiceDate >= ((DateTime?)@from).Value).WhereIf(toExclusive.HasValue, (StockPilot.Entity.PurchaseInvoice d) => d.InvoiceDate < ((DateTime?)toExclusive).Value)
						.WhereIf(input.SupplierId.HasValue, (StockPilot.Entity.PurchaseInvoice d) => d.SupplierId == input.SupplierId.Value)
						.WhereIf(input.Status.HasValue, (StockPilot.Entity.PurchaseInvoice d) => (int)d.Status == (int)input.Status.Value)
					orderby d.InvoiceDate, d.InvoiceNo
					select d).Take(100000)
				select new InvoiceListRowDto
				{
					InvoiceId = d.Id,
					InvoiceNo = d.InvoiceNo,
					InvoiceDate = d.InvoiceDate,
					SupplierName = d.SupplierInfo.Name,
					SupplierInvoiceNo = d.SupplierInvoiceNo,
					CurrencyCode = d.CurrencyInfo.Code,
					ExchangeRate = d.ExchangeRate,
					LineCount = d.Items.Count((PurchaseInvoiceItem l) => !l.IsDeleted),
					SubTotal = d.SubTotal,
					DiscountAmount = d.DiscountAmount,
					TaxAmount = d.TaxAmount,
					OtherCharges = d.OtherCharges,
					TotalAmount = d.TotalAmount,
					Status = d.Status
				}).ToListAsync();
			foreach (InvoiceListRowDto row in rows)
			{
				row.TotalInBaseCurrency = StockManager.Round(row.TotalAmount * row.ExchangeRate);
			}
			return new InvoiceListReportDto
			{
				Rows = rows,
				InvoiceCount = rows.Count,
				PostedCount = rows.Count((InvoiceListRowDto r) => r.Status == DocumentStatus.Posted),
				PostedTotal = rows.Where((InvoiceListRowDto r) => r.Status == DocumentStatus.Posted).Sum((InvoiceListRowDto r) => r.TotalInBaseCurrency),
				DraftTotal = rows.Where((InvoiceListRowDto r) => r.Status == DocumentStatus.Draft).Sum((InvoiceListRowDto r) => r.TotalInBaseCurrency)
			};
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetInvoiceListToExcel(InvoiceListInput input)
		{
			InventoryReportExcelExporter exporter = _exporter;
			return exporter.ExportInvoiceList(await GetInvoiceList(input));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		public async Task<SalesListReportDto> GetSalesList(SalesListInput input)
		{
			DateTime? from = input.DateFrom?.Date;
			DateTime? toExclusive = input.DateTo?.Date.AddDays(1.0);
			string customer = input.CustomerName?.Trim();
			List<SalesListRowDto> rows = await (from d in (from d in _issueSaleRepository.GetAll().WhereIf(@from.HasValue, (StockPilot.Entity.IssueSale d) => d.DocumentDate >= ((DateTime?)@from).Value).WhereIf(toExclusive.HasValue, (StockPilot.Entity.IssueSale d) => d.DocumentDate < ((DateTime?)toExclusive).Value)
						.WhereIf(input.IssueType.HasValue, (StockPilot.Entity.IssueSale d) => (int)d.IssueType == (int)input.IssueType.Value)
						.WhereIf(!customer.IsNullOrEmpty(), (StockPilot.Entity.IssueSale d) => d.CustomerName == customer)
						.WhereIf(input.Status.HasValue, (StockPilot.Entity.IssueSale d) => (int)d.Status == (int)input.Status.Value)
					orderby d.DocumentDate, d.DocumentNo
					select d).Take(100000)
				select new SalesListRowDto
				{
					DocumentId = d.Id,
					DocumentNo = d.DocumentNo,
					DocumentDate = d.DocumentDate,
					IssueType = d.IssueType,
					CustomerName = d.CustomerName,
					LineCount = d.Items.Count((IssueSalesItem l) => !l.IsDeleted),
					CurrencyCode = d.CurrencyInfo.Code,
					ExchangeRate = d.ExchangeRate,
					SubTotal = d.SubTotal,
					DiscountAmount = d.DiscountAmount,
					TaxAmount = d.TaxAmount,
					TotalAmount = d.TotalAmount,
					Cost = (d.Items.Where((IssueSalesItem l) => !l.IsDeleted).Sum((IssueSalesItem l) => (decimal?)(l.BaseQuantity * l.UnitCost)) ?? 0m),
					Status = d.Status
				}).ToListAsync();
			foreach (SalesListRowDto row in rows)
			{
				row.Cost = StockManager.Round(row.Cost);
				if (row.IssueType == IssueSaleType.Sale)
				{
					row.NetSalesInBaseCurrency = StockManager.Round((row.SubTotal - row.DiscountAmount) * row.ExchangeRate);
					if (row.Status == DocumentStatus.Posted)
					{
						row.Margin = row.NetSalesInBaseCurrency - row.Cost;
						row.MarginPercent = Percent(row.Margin.Value, row.NetSalesInBaseCurrency);
					}
				}
			}
			List<SalesListRowDto> postedSales = rows.Where((SalesListRowDto r) => r.Status == DocumentStatus.Posted && r.IssueType == IssueSaleType.Sale).ToList();
			SalesListReportDto report = new SalesListReportDto
			{
				Rows = rows,
				DocumentCount = rows.Count,
				PostedCount = rows.Count((SalesListRowDto r) => r.Status == DocumentStatus.Posted),
				PostedSales = postedSales.Sum((SalesListRowDto r) => r.NetSalesInBaseCurrency),
				PostedCostOfSales = postedSales.Sum((SalesListRowDto r) => r.Cost),
				PostedIssueCost = rows.Where((SalesListRowDto r) => r.Status == DocumentStatus.Posted && r.IssueType != IssueSaleType.Sale).Sum((SalesListRowDto r) => r.Cost)
			};
			report.PostedMargin = report.PostedSales - report.PostedCostOfSales;
			report.PostedMarginPercent = Percent(report.PostedMargin, report.PostedSales);
			return report;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetSalesListToExcel(SalesListInput input)
		{
			InventoryReportExcelExporter exporter = _exporter;
			return exporter.ExportSalesList(await GetSalesList(input));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		public async Task<InvoiceToSalesReportDto> GetInvoiceToSales(InvoiceToSalesInput input)
		{
			DateTime? from = input.DateFrom?.Date;
			DateTime? toExclusive = input.DateTo?.Date.AddDays(1.0);
			var lots = await (from x in (from m in _movementRepository.GetAll()
					join d in _invoiceRepository.GetAll() on m.ReferenceId equals d.Id
					where (int)m.MovementType == 2 && m.ReferenceType == "PurchaseInvoice"
					select new
					{
						Movement = m,
						InvoiceNo = d.InvoiceNo,
						InvoiceDate = d.InvoiceDate,
						SupplierId = d.SupplierId,
						SupplierName = d.SupplierInfo.Name,
						InvoiceId = d.Id
					}).WhereIf(@from.HasValue, x => x.InvoiceDate >= ((DateTime?)@from).Value).WhereIf(toExclusive.HasValue, x => x.InvoiceDate < ((DateTime?)toExclusive).Value).WhereIf(input.SupplierId.HasValue, x => x.SupplierId == input.SupplierId.Value)
					.WhereIf(input.ItemMasterId.HasValue, x => x.Movement.ItemMasterId == input.ItemMasterId.Value)
				orderby x.InvoiceDate, x.InvoiceNo, x.Movement.Id
				select x).Take(100000).ToListAsync();
			InvoiceToSalesReportDto report = new InvoiceToSalesReportDto
			{
				Note = L("InvoiceToSalesNote")
			};
			if (!lots.Any())
			{
				return report;
			}
			List<int> itemIds = lots.Select(l => l.Movement.ItemMasterId).Distinct().ToList();
			List<StockPilot.Entity.StockMovement> history = await (from m in _movementRepository.GetAll()
				where itemIds.Contains(m.ItemMasterId)
				orderby m.Id
				select m).ToListAsync();
			Dictionary<long, StockPilot.Entity.StockMovement> historyById = history.ToDictionary((StockPilot.Entity.StockMovement m) => m.Id);
			Dictionary<int, StockPilot.Entity.ItemMaster> itemsById = (await (from i in _itemRepository.GetAll().Include((StockPilot.Entity.ItemMaster i) => i.BaseUnit)
				where itemIds.Contains(i.Id)
				select i).ToListAsync()).ToDictionary((StockPilot.Entity.ItemMaster i) => i.Id);
			Dictionary<long, List<FifoAllocation>> allocationsByLot = new Dictionary<long, List<FifoAllocation>>();
			Dictionary<int, FifoResult> results = new Dictionary<int, FifoResult>();
			foreach (IGrouping<int, StockPilot.Entity.StockMovement> group in from m in history
				group m by m.ItemMasterId)
			{
				FifoResult result = FifoAllocator.Allocate(group.Select((StockPilot.Entity.StockMovement m) => new FifoMovement
				{
					Id = m.Id,
					MovementType = m.MovementType,
					Quantity = m.Quantity,
					UnitCost = m.UnitCost,
					ReferenceType = m.ReferenceType,
					ReferenceId = m.ReferenceId,
					ReferenceItemId = m.ReferenceItemId
				}));
				results[group.Key] = result;
				foreach (FifoAllocation allocation in result.Allocations.Where((FifoAllocation a) => a.InMovementId.HasValue))
				{
					if (!allocationsByLot.TryGetValue(allocation.InMovementId.Value, out var list))
					{
						list = (allocationsByLot[allocation.InMovementId.Value] = new List<FifoAllocation>());
					}
					list.Add(allocation);
				}
			}
			foreach (var lot in lots)
			{
				StockPilot.Entity.StockMovement movement = lot.Movement;
				StockPilot.Entity.ItemMaster item = itemsById[movement.ItemMasterId];
				FifoResult fifoResult = results[movement.ItemMasterId];
				allocationsByLot.TryGetValue(movement.Id, out var allocations);
				allocations = allocations ?? new List<FifoAllocation>();
				fifoResult.RemainingByLot.TryGetValue(movement.Id, out var remaining);
				fifoResult.ReturnedByLot.TryGetValue(movement.Id, out var returned);
				InvoiceToSalesRowDto row = new InvoiceToSalesRowDto
				{
					InvoiceId = lot.InvoiceId,
					InvoiceNo = lot.InvoiceNo,
					InvoiceDate = lot.InvoiceDate,
					SupplierName = lot.SupplierName,
					ItemId = item.Id,
					ItemCode = item.ItemCode,
					ItemName = item.ItemName,
					BaseUnitCode = item.BaseUnit?.Code,
					ReceivedQuantity = movement.Quantity,
					UnitCost = movement.UnitCost,
					ReturnedQuantity = returned,
					RemainingQuantity = remaining,
					CurrentSalePrice = item.SalePrice,
					Allocations = allocations.Select(delegate(FifoAllocation a)
					{
						StockPilot.Entity.StockMovement stockMovement = historyById[a.OutMovementId];
						return new InvoiceToSalesAllocationDto
						{
							OutMovementId = stockMovement.Id,
							MovementType = stockMovement.MovementType,
							MovementDate = stockMovement.MovementDate,
							ReferenceType = stockMovement.ReferenceType,
							ReferenceId = stockMovement.ReferenceId,
							ReferenceNo = stockMovement.ReferenceNo,
							Quantity = a.Quantity
						};
					}).ToList()
				};
				row.SoldQuantity = row.Allocations.Where((InvoiceToSalesAllocationDto a) => a.MovementType == StockMovementType.SaleOut).Sum((InvoiceToSalesAllocationDto a) => a.Quantity);
				row.IssuedQuantity = row.Allocations.Where((InvoiceToSalesAllocationDto a) => a.MovementType == StockMovementType.IssueOut).Sum((InvoiceToSalesAllocationDto a) => a.Quantity);
				row.OtherOutQuantity = row.Allocations.Where((InvoiceToSalesAllocationDto a) => a.MovementType != StockMovementType.SaleOut && a.MovementType != StockMovementType.IssueOut).Sum((InvoiceToSalesAllocationDto a) => a.Quantity);
				row.CostOfSold = StockManager.Round(row.SoldQuantity * row.UnitCost);
				row.SalesValue = StockManager.Round(row.SoldQuantity * row.CurrentSalePrice);
				row.Margin = row.SalesValue - row.CostOfSold;
				row.MarginPercent = Percent(row.Margin, row.SalesValue);
				report.Rows.Add(row);
			}
			report.TotalCostOfSold = report.Rows.Sum((InvoiceToSalesRowDto r) => r.CostOfSold);
			report.TotalSalesValue = report.Rows.Sum((InvoiceToSalesRowDto r) => r.SalesValue);
			report.TotalMargin = report.TotalSalesValue - report.TotalCostOfSold;
			return report;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Reports" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetInvoiceToSalesToExcel(InvoiceToSalesInput input)
		{
			InventoryReportExcelExporter exporter = _exporter;
			return exporter.ExportInvoiceToSales(await GetInvoiceToSales(input));
		}

		private static decimal? Percent(decimal part, decimal whole)
		{
			if (!(whole != 0m))
			{
				return null;
			}
			return Math.Round(part / whole, 4);
		}
	}
}
