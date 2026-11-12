using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class StockManager : StockPilotDomainServiceBase
	{
		private class PostingContext
		{
			private readonly Dictionary<int, ItemMaster> _items = new Dictionary<int, ItemMaster>();

			private readonly Dictionary<int, StockBalance> _balances = new Dictionary<int, StockBalance>();

			public async Task<ItemMaster> GetItemAsync(IRepository<ItemMaster> repository, int itemId)
			{
				if (!_items.TryGetValue(itemId, out var item))
				{
					item = await repository.GetAsync(itemId);
					_items[itemId] = item;
				}
				return item;
			}

			public async Task<StockBalance> GetOrCreateBalanceAsync(StockManager manager, ItemMaster item)
			{
				if (_balances.TryGetValue(item.Id, out var balance))
				{
					return balance;
				}
				balance = await manager._balanceRepository.FirstOrDefaultAsync((StockBalance b) => b.ItemMasterId == item.Id);
				if (balance == null)
				{
					balance = new StockBalance
					{
						TenantId = item.TenantId,
						ItemMasterId = item.Id
					};
					await manager._balanceRepository.InsertAsync(balance);
				}
				_balances[item.Id] = balance;
				return balance;
			}
		}

		private const int Scale = 4;

		private readonly IRepository<PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<PurchaseInvoiceItem> _purchaseInvoiceItemRepository;

		private readonly IRepository<IssueSale> _issueSaleRepository;

		private readonly IRepository<IssueSalesItem> _issueSalesItemRepository;

		private readonly IRepository<ItemMaster> _itemRepository;

		private readonly IRepository<UnitMaster> _unitRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly IRepository<StockMovement, long> _movementRepository;

		private readonly UnitConversionManager _unitConversionManager;

		public IAbpSession AbpSession { get; set; }

		public StockManager(IRepository<PurchaseInvoice> purchaseInvoiceRepository, IRepository<PurchaseInvoiceItem> purchaseInvoiceItemRepository, IRepository<IssueSale> issueSaleRepository, IRepository<IssueSalesItem> issueSalesItemRepository, IRepository<ItemMaster> itemRepository, IRepository<UnitMaster> unitRepository, IRepository<StockBalance> balanceRepository, IRepository<StockMovement, long> movementRepository, UnitConversionManager unitConversionManager)
		{
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_purchaseInvoiceItemRepository = purchaseInvoiceItemRepository;
			_issueSaleRepository = issueSaleRepository;
			_issueSalesItemRepository = issueSalesItemRepository;
			_itemRepository = itemRepository;
			_unitRepository = unitRepository;
			_balanceRepository = balanceRepository;
			_movementRepository = movementRepository;
			_unitConversionManager = unitConversionManager;
			AbpSession = NullAbpSession.Instance;
		}

		[UnitOfWork]
		public virtual async Task PostPurchaseAsync(int invoiceId)
		{
			PurchaseInvoice invoice = await _purchaseInvoiceRepository.GetAsync(invoiceId);
			EnsureDraft(invoice.Status, invoice.InvoiceNo);
			decimal exchangeRate = EnsureExchangeRate(invoice.ExchangeRate, invoice.InvoiceNo);
			List<PurchaseInvoiceItem> lines = await GetLinesAsync(invoice);
			PostingContext context = new PostingContext();
			foreach (PurchaseInvoiceItem line in lines)
			{
				ItemMaster item = await context.GetItemAsync(_itemRepository, line.ItemMasterId);
				await ApplyConversionAsync(item, line.UnitMasterId, line.Quantity, delegate(decimal factor)
				{
					line.ConversionFactor = factor;
					line.BaseQuantity = Round(line.Quantity * factor);
				});
				if (item.IsStockItem)
				{
					decimal netValue = line.Quantity * line.UnitCost - line.DiscountAmount;
					if (netValue < 0m)
					{
						throw new UserFriendlyException(L("LineValueCannotBeNegative", item.ItemName));
					}
					decimal baseUnitCost = Round(netValue * exchangeRate / line.BaseQuantity);
					StockBalance balance = await context.GetOrCreateBalanceAsync(this, item);
					ApplyIncoming(balance, line.BaseQuantity, baseUnitCost);
					balance.LastPurchaseCost = baseUnitCost;
					await AddMovementAsync(balance, StockMovementType.PurchaseIn, invoice.InvoiceDate, line.BaseQuantity, baseUnitCost, "PurchaseInvoice", invoice.Id, line.Id, invoice.InvoiceNo, null);
				}
			}
			invoice.Status = DocumentStatus.Posted;
			invoice.IsPosted = true;
			invoice.PostedBy = AbpSession.UserId;
			invoice.PostedTime = Clock.Now;
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task CancelPurchaseAsync(int invoiceId)
		{
			PurchaseInvoice invoice = await _purchaseInvoiceRepository.GetAsync(invoiceId);
			EnsurePosted(invoice.Status, invoice.InvoiceNo);
			List<StockMovement> postedMovements = await _movementRepository.GetAllListAsync((StockMovement m) => m.ReferenceType == "PurchaseInvoice" && m.ReferenceId == (int?)invoice.Id && (int)m.MovementType == 2);
			PostingContext context = new PostingContext();
			foreach (IGrouping<int, StockMovement> group in from m in postedMovements
				group m by m.ItemMasterId)
			{
				long lastPostedId = group.Max((StockMovement m) => m.Id);
				decimal receivedQuantity = group.Sum((StockMovement m) => m.Quantity);
				int itemId = group.Key;
				int usedSince = await _movementRepository.CountAsync((StockMovement m) => m.ItemMasterId == itemId && m.Id > lastPostedId && m.Quantity < 0m);
				PostingContext postingContext = context;
				StockBalance balance = await postingContext.GetOrCreateBalanceAsync(this, await context.GetItemAsync(_itemRepository, itemId));
				if (usedSince > 0 || balance.QuantityOnHand < receivedQuantity)
				{
					ItemMaster item = await context.GetItemAsync(_itemRepository, itemId);
					throw new UserFriendlyException(L("CannotCancelPurchaseStockUsed", invoice.InvoiceNo, item.ItemName));
				}
			}
			foreach (StockMovement posted in postedMovements.OrderBy((StockMovement m) => m.Id))
			{
				StockBalance balance2 = await context.GetOrCreateBalanceAsync(this, await context.GetItemAsync(_itemRepository, posted.ItemMasterId));
				ApplyIncomingReversal(balance2, posted.Quantity, posted.UnitCost);
				await AddMovementAsync(balance2, StockMovementType.PurchaseReturnOut, Clock.Now, -posted.Quantity, posted.UnitCost, "PurchaseInvoice", invoice.Id, posted.ReferenceItemId, invoice.InvoiceNo, L("CancelledDocumentRemark"));
			}
			invoice.Status = DocumentStatus.Cancelled;
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task PostIssueSaleAsync(int issueSaleId)
		{
			IssueSale document = await _issueSaleRepository.GetAsync(issueSaleId);
			EnsureDraft(document.Status, document.DocumentNo);
			List<IssueSalesItem> lines = await GetLinesAsync(document);
			PostingContext context = new PostingContext();
			List<IssueSalesItem> stockLines = new List<IssueSalesItem>();
			foreach (IssueSalesItem line in lines)
			{
				ItemMaster item = await context.GetItemAsync(_itemRepository, line.ItemMasterId);
				await ApplyConversionAsync(item, line.UnitMasterId, line.Quantity, delegate(decimal factor)
				{
					line.ConversionFactor = factor;
					line.BaseQuantity = Round(line.Quantity * factor);
				});
				if (item.IsStockItem)
				{
					stockLines.Add(line);
				}
				else
				{
					line.UnitCost = 0m;
				}
			}
			List<string> shortages = new List<string>();
			List<string> shortItemNames = new List<string>();
			foreach (IGrouping<int, IssueSalesItem> group in from l in stockLines
				group l by l.ItemMasterId)
			{
				ItemMaster item = await context.GetItemAsync(_itemRepository, group.Key);
				StockBalance balance = await context.GetOrCreateBalanceAsync(this, item);
				decimal required = group.Sum((IssueSalesItem l) => l.BaseQuantity);
				if (balance.AvailableQuantity < required)
				{
					UnitMaster unit = await _unitRepository.FirstOrDefaultAsync(item.BaseUnitId);
					shortItemNames.Add(item.ItemName);
					shortages.Add(L("ItemShortDetail", item.ItemName, FormatQuantity(required), FormatQuantity(balance.AvailableQuantity), unit?.Code ?? string.Empty));
				}
			}
			if (shortages.Any())
			{
				throw new UserFriendlyException(L("InsufficientStockForItems", string.Join(", ", shortItemNames)), string.Join(Environment.NewLine, shortages));
			}
			StockMovementType movementType = ((document.IssueType == IssueSaleType.Sale) ? StockMovementType.SaleOut : StockMovementType.IssueOut);
			foreach (IssueSalesItem line2 in stockLines)
			{
				StockBalance balance2 = await context.GetOrCreateBalanceAsync(this, await context.GetItemAsync(_itemRepository, line2.ItemMasterId));
				line2.UnitCost = balance2.AverageCost;
				ApplyOutgoing(balance2, line2.BaseQuantity);
				await AddMovementAsync(balance2, movementType, document.DocumentDate, -line2.BaseQuantity, line2.UnitCost, "IssueSale", document.Id, line2.Id, document.DocumentNo, null);
			}
			document.Status = DocumentStatus.Posted;
			document.IsPosted = true;
			document.PostedBy = AbpSession.UserId;
			document.PostedTime = Clock.Now;
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task CancelIssueSaleAsync(int issueSaleId)
		{
			IssueSale document = await _issueSaleRepository.GetAsync(issueSaleId);
			EnsurePosted(document.Status, document.DocumentNo);
			List<StockMovement> postedMovements = await _movementRepository.GetAllListAsync((StockMovement m) => m.ReferenceType == "IssueSale" && m.ReferenceId == (int?)document.Id && ((int)m.MovementType == 4 || (int)m.MovementType == 6));
			PostingContext context = new PostingContext();
			foreach (StockMovement posted in postedMovements.OrderBy((StockMovement m) => m.Id))
			{
				StockBalance balance = await context.GetOrCreateBalanceAsync(this, await context.GetItemAsync(_itemRepository, posted.ItemMasterId));
				decimal quantity = -posted.Quantity;
				ApplyIncoming(balance, quantity, posted.UnitCost);
				await AddMovementAsync(balance, StockMovementType.SaleReturnIn, Clock.Now, quantity, posted.UnitCost, "IssueSale", document.Id, posted.ReferenceItemId, document.DocumentNo, L("CancelledDocumentRemark"));
			}
			document.Status = DocumentStatus.Cancelled;
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task PostOpeningAsync(int itemId, decimal quantity, decimal unitCost, DateTime? date = null)
		{
			ItemMaster item = await _itemRepository.GetAsync(itemId);
			EnsureStockItem(item);
			if (quantity <= 0m)
			{
				throw new UserFriendlyException(L("QuantityMustBePositive"));
			}
			if (unitCost < 0m)
			{
				throw new UserFriendlyException(L("CostCannotBeNegative"));
			}
			if (await _movementRepository.CountAsync((StockMovement m) => m.ItemMasterId == itemId) > 0)
			{
				throw new UserFriendlyException(L("OpeningOnlyBeforeOtherMovements", item.ItemName));
			}
			quantity = Round(quantity);
			unitCost = Round(unitCost);
			StockBalance balance = await new PostingContext().GetOrCreateBalanceAsync(this, item);
			ApplyIncoming(balance, quantity, unitCost);
			if (balance.LastPurchaseCost == 0m)
			{
				balance.LastPurchaseCost = unitCost;
			}
			await AddMovementAsync(balance, StockMovementType.Opening, date ?? Clock.Now, quantity, unitCost, "Opening", null, null, null, null);
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task AdjustAsync(int itemId, decimal quantity, string reason, DateTime? date = null)
		{
			ItemMaster item = await _itemRepository.GetAsync(itemId);
			EnsureStockItem(item);
			if (quantity == 0m)
			{
				throw new UserFriendlyException(L("AdjustmentQuantityCannotBeZero"));
			}
			if (string.IsNullOrWhiteSpace(reason))
			{
				throw new UserFriendlyException(L("AdjustmentReasonRequired"));
			}
			quantity = Round(quantity);
			StockBalance balance = await new PostingContext().GetOrCreateBalanceAsync(this, item);
			decimal unitCost = ((balance.AverageCost > 0m) ? balance.AverageCost : Round(item.StandardCost));
			if (quantity > 0m)
			{
				ApplyIncoming(balance, quantity, unitCost);
				await AddMovementAsync(balance, StockMovementType.AdjustmentIn, date ?? Clock.Now, quantity, unitCost, "Adjustment", null, null, null, reason.Trim());
			}
			else
			{
				decimal outQuantity = -quantity;
				if (balance.AvailableQuantity < outQuantity)
				{
					UnitMaster unit = await _unitRepository.FirstOrDefaultAsync(item.BaseUnitId);
					throw new UserFriendlyException(L("InsufficientStock"), L("ItemShortDetail", item.ItemName, FormatQuantity(outQuantity), FormatQuantity(balance.AvailableQuantity), unit?.Code ?? string.Empty));
				}
				ApplyOutgoing(balance, outQuantity);
				await AddMovementAsync(balance, StockMovementType.AdjustmentOut, date ?? Clock.Now, quantity, unitCost, "Adjustment", null, null, null, reason.Trim());
			}
			await SaveAsync();
		}

		[UnitOfWork]
		public virtual async Task EnsureBalanceAsync(int itemId)
		{
			ItemMaster item = await _itemRepository.GetAsync(itemId);
			await new PostingContext().GetOrCreateBalanceAsync(this, item);
			await SaveAsync();
		}

		public static void ApplyIncoming(StockBalance balance, decimal quantity, decimal unitCost)
		{
			decimal oldQuantity = balance.QuantityOnHand;
			decimal newQuantity = oldQuantity + quantity;
			if (oldQuantity <= 0m || newQuantity <= 0m)
			{
				balance.AverageCost = Round(unitCost);
			}
			else
			{
				balance.AverageCost = Round((oldQuantity * balance.AverageCost + quantity * unitCost) / newQuantity);
			}
			balance.QuantityOnHand = Round(newQuantity);
			balance.LastMovementTime = Clock.Now;
		}

		public static void ApplyOutgoing(StockBalance balance, decimal quantity)
		{
			balance.QuantityOnHand = Round(balance.QuantityOnHand - quantity);
			balance.LastMovementTime = Clock.Now;
		}

		public static void ApplyIncomingReversal(StockBalance balance, decimal quantity, decimal unitCost)
		{
			decimal oldQuantity = balance.QuantityOnHand;
			decimal newQuantity = oldQuantity - quantity;
			if (newQuantity > 0m)
			{
				decimal average = (oldQuantity * balance.AverageCost - quantity * unitCost) / newQuantity;
				balance.AverageCost = Round(Math.Max(average, 0m));
			}
			balance.QuantityOnHand = Round(newQuantity);
			balance.LastMovementTime = Clock.Now;
		}

		public static decimal Round(decimal value)
		{
			return Math.Round(value, 4, MidpointRounding.AwayFromZero);
		}

		private async Task<List<PurchaseInvoiceItem>> GetLinesAsync(PurchaseInvoice invoice)
		{
			List<PurchaseInvoiceItem> lines = (await _purchaseInvoiceItemRepository.GetAllListAsync((PurchaseInvoiceItem l) => l.PurchaseInvoiceId == invoice.Id)).OrderBy((PurchaseInvoiceItem l) => l.Id).ToList();
			if (!lines.Any())
			{
				throw new UserFriendlyException(L("DocumentHasNoLines", invoice.InvoiceNo));
			}
			return lines;
		}

		private async Task<List<IssueSalesItem>> GetLinesAsync(IssueSale document)
		{
			List<IssueSalesItem> lines = (await _issueSalesItemRepository.GetAllListAsync((IssueSalesItem l) => l.IssueSaleId == document.Id)).OrderBy((IssueSalesItem l) => l.Id).ToList();
			if (!lines.Any())
			{
				throw new UserFriendlyException(L("DocumentHasNoLines", document.DocumentNo));
			}
			return lines;
		}

		private async Task ApplyConversionAsync(ItemMaster item, int unitId, decimal quantity, Action<decimal> apply)
		{
			if (quantity <= 0m)
			{
				throw new UserFriendlyException(L("LineQuantityMustBePositive", item.ItemName));
			}
			apply(await _unitConversionManager.GetFactorAsync(item, unitId));
		}

		private async Task AddMovementAsync(StockBalance balance, StockMovementType type, DateTime date, decimal signedQuantity, decimal unitCost, string referenceType, int? referenceId, int? referenceItemId, string referenceNo, string remarks)
		{
			await _movementRepository.InsertAsync(new StockMovement
			{
				TenantId = balance.TenantId,
				ItemMasterId = balance.ItemMasterId,
				MovementDate = date,
				MovementType = type,
				Quantity = signedQuantity,
				UnitCost = unitCost,
				TotalValue = Round(signedQuantity * unitCost),
				BalanceAfter = balance.QuantityOnHand,
				ReferenceType = referenceType,
				ReferenceId = referenceId,
				ReferenceItemId = referenceItemId,
				ReferenceNo = referenceNo,
				Remarks = remarks
			});
		}

		private async Task SaveAsync()
		{
			try
			{
				await base.CurrentUnitOfWork.SaveChangesAsync();
			}
			catch (Exception exception) when (DbExceptionHelper.IsSaveConflict(exception))
			{
				base.Logger.Warn("Stock save conflict.", exception);
				throw new UserFriendlyException(L("StockChangedByAnotherUser"));
			}
		}

		private void EnsureDraft(DocumentStatus status, string documentNo)
		{
			if (status != DocumentStatus.Draft)
			{
				throw new UserFriendlyException(L("DocumentMustBeDraftToPost", documentNo));
			}
		}

		private void EnsurePosted(DocumentStatus status, string documentNo)
		{
			if (status != DocumentStatus.Posted)
			{
				throw new UserFriendlyException(L("DocumentMustBePostedToCancel", documentNo));
			}
		}

		private decimal EnsureExchangeRate(decimal exchangeRate, string documentNo)
		{
			if (exchangeRate <= 0m)
			{
				throw new UserFriendlyException(L("ExchangeRateMustBePositive", documentNo));
			}
			return exchangeRate;
		}

		private void EnsureStockItem(ItemMaster item)
		{
			if (!item.IsStockItem)
			{
				throw new UserFriendlyException(L("ItemIsNotStockItem", item.ItemName));
			}
		}

		private static string FormatQuantity(decimal value)
		{
			return value.ToString("0.####", CultureInfo.InvariantCulture);
		}
	}
}
