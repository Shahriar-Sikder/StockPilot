using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.PurchaseInvoice;
using StockPilot.Services.PurchaseInvoice.Dtos;
using StockPilot.Services.PurchaseInvoiceImport.Dtos;
using StockPilot.Services.PurchaseInvoiceImport.Exporting;

namespace StockPilot.Services.PurchaseInvoiceImport
{
	[AbpAuthorize(new string[] { })]
	[RequiresFeature(new string[] { "App.Inventory.Purchases", "App.Inventory.Purchases.ExcelImport" }, RequiresAll = true)]
	public class PurchaseInvoiceImportAppService : StockPilotAppServiceBase, IPurchaseInvoiceImportAppService, IApplicationService, ITransientDependency
	{
		private static readonly string[] InvoiceNoHeaders = new string[6] { "invoiceno", "documentno", "docno", "invoicenumber", "documentnumber", "invoice" };

		private static readonly string[] DateHeaders = new string[3] { "invoicedate", "date", "documentdate" };

		private static readonly string[] SupplierHeaders = new string[3] { "suppliercode", "supplier", "suppliername" };

		private static readonly string[] CurrencyHeaders = new string[2] { "currency", "currencycode" };

		private static readonly string[] ItemHeaders = new string[3] { "itemcode", "item", "sku" };

		private static readonly string[] UnitHeaders = new string[3] { "unit", "uom", "unitcode" };

		private static readonly string[] QuantityHeaders = new string[2] { "quantity", "qty" };

		private static readonly string[] CostHeaders = new string[4] { "unitcost", "cost", "price", "unitprice" };

		private const int ExistingNumberBatchSize = 1000;

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		private readonly IRepository<StockPilot.Entity.Currency> _currencyRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _invoiceRepository;

		private readonly UnitConversionManager _unitConversionManager;

		private readonly ExchangeRateManager _exchangeRateManager;

		private readonly IPurchaseInvoiceAppService _purchaseInvoiceAppService;

		private readonly StockManager _stockManager;

		private readonly PurchaseImportTemplateExporter _templateExporter;

		public PurchaseInvoiceImportAppService(IRepository<StockPilot.Entity.Supplier> supplierRepository, IRepository<StockPilot.Entity.Currency> currencyRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository, IRepository<StockPilot.Entity.PurchaseInvoice> invoiceRepository, UnitConversionManager unitConversionManager, ExchangeRateManager exchangeRateManager, IPurchaseInvoiceAppService purchaseInvoiceAppService, StockManager stockManager, PurchaseImportTemplateExporter templateExporter)
		{
			_supplierRepository = supplierRepository;
			_currencyRepository = currencyRepository;
			_itemRepository = itemRepository;
			_invoiceRepository = invoiceRepository;
			_unitConversionManager = unitConversionManager;
			_exchangeRateManager = exchangeRateManager;
			_purchaseInvoiceAppService = purchaseInvoiceAppService;
			_stockManager = stockManager;
			_templateExporter = templateExporter;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Create" })]
		public FileDto GetTemplate()
		{
			return _templateExporter.CreateTemplate();
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Create" })]
		public async Task<PurchaseImportPreviewDto> Preview(PurchaseImportFileInput input)
		{
			List<PurchaseImportInvoiceDto> invoices = (await BuildCandidatesAsync(input)).Select((PurchaseImportCandidate c) => c.Preview).ToList();
			return new PurchaseImportPreviewDto
			{
				Invoices = invoices,
				ReadyCount = invoices.Count((PurchaseImportInvoiceDto i) => i.Status == PurchaseImportStatus.Ready),
				ExistsCount = invoices.Count((PurchaseImportInvoiceDto i) => i.Status == PurchaseImportStatus.Exists),
				ErrorCount = invoices.Count((PurchaseImportInvoiceDto i) => i.Status == PurchaseImportStatus.Error)
			};
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Create" })]
		public async Task<PurchaseImportResultDto> Import(ImportPurchaseInvoicesInput input)
		{
			if (input.PostAfterImport)
			{
				await base.PermissionChecker.AuthorizeAsync("Pages.StockPilot.Purchases.Post");
			}
			PurchaseImportResultDto result = new PurchaseImportResultDto();
			foreach (PurchaseImportCandidate candidate in await BuildCandidatesAsync(input))
			{
				if (candidate.Preview.Status != PurchaseImportStatus.Ready)
				{
					result.Skipped.Add(candidate.Preview);
					continue;
				}
				EntityDto created = await _purchaseInvoiceAppService.Create(candidate.Invoice);
				if (input.PostAfterImport)
				{
					await _stockManager.PostPurchaseAsync(created.Id);
				}
				result.Created.Add(new PurchaseImportCreatedInvoiceDto
				{
					Id = created.Id,
					InvoiceNo = candidate.Invoice.InvoiceNo,
					IsPosted = input.PostAfterImport
				});
			}
			return result;
		}

		private async Task<List<PurchaseImportCandidate>> BuildCandidatesAsync(PurchaseImportFileInput input)
		{
			List<PurchaseImportRow> rows = ReadRows(input);
			List<StockPilot.Entity.Supplier> suppliers = await _supplierRepository.GetAllListAsync();
			List<StockPilot.Entity.Currency> currencies = await _currencyRepository.GetAllListAsync();
			List<StockPilot.Entity.ItemMaster> items = await _itemRepository.GetAllListAsync();
			StockPilot.Entity.Currency baseCurrency = currencies.FirstOrDefault((StockPilot.Entity.Currency c) => c.IsBaseCurrency);
			Dictionary<int, List<ItemUnitInfo>> unitsByItem = new Dictionary<int, List<ItemUnitInfo>>();
			List<PurchaseImportCandidate> candidates = new List<PurchaseImportCandidate>();
			Dictionary<string, PurchaseImportCandidate> byNumber = new Dictionary<string, PurchaseImportCandidate>(StringComparer.OrdinalIgnoreCase);
			foreach (PurchaseImportRow row in rows)
			{
				string invoiceNo = row.GetText(InvoiceNoHeaders);
				string itemText = row.GetText(ItemHeaders);
				if (invoiceNo.Length != 0 || itemText.Length != 0)
				{
					if (!byNumber.TryGetValue(invoiceNo, out var candidate))
					{
						candidate = (byNumber[invoiceNo] = new PurchaseImportCandidate(invoiceNo));
						candidates.Add(candidate);
					}
					await AddRowAsync(candidate, row, invoiceNo, itemText, suppliers, currencies, items, unitsByItem);
				}
			}
			HashSet<string> existing = await FindExistingNumbersAsync((from c in candidates
				select c.Invoice.InvoiceNo into n
				where n.Length > 0
				select n).ToList());
			foreach (PurchaseImportCandidate candidate2 in candidates)
			{
				await CompleteAsync(candidate2, currencies, baseCurrency, existing);
			}
			return candidates;
		}

		private List<PurchaseImportRow> ReadRows(PurchaseImportFileInput input)
		{
			if (!PurchaseImportFileReader.IsSupported(input.FileName))
			{
				throw new UserFriendlyException(L("ImportFileTypeNotSupported"));
			}
			byte[] bytes = PictureHelper.DecodeBase64(input.Base64Content);
			if (bytes == null || bytes.Length == 0)
			{
				throw new UserFriendlyException(L("ImportFileInvalid"));
			}
			if (bytes.Length > 3145728)
			{
				throw new UserFriendlyException(L("ImportFileTooLarge", 3));
			}
			try
			{
				return PurchaseImportFileReader.Read(input.FileName, bytes);
			}
			catch (InvalidDataException ex) when (ex.Message == "TooManyRows")
			{
				throw new UserFriendlyException(L("ImportTooManyRows", 5000));
			}
			catch (Exception ex2) when (!(ex2 is UserFriendlyException))
			{
				base.Logger.Warn("Could not read import file " + input.FileName, ex2);
				throw new UserFriendlyException(L("ImportFileUnreadable"));
			}
		}

		private async Task AddRowAsync(PurchaseImportCandidate candidate, PurchaseImportRow row, string invoiceNo, string itemText, List<StockPilot.Entity.Supplier> suppliers, List<StockPilot.Entity.Currency> currencies, List<StockPilot.Entity.ItemMaster> items, Dictionary<int, List<ItemUnitInfo>> unitsByItem)
		{
			int rowNumber = row.RowNumber;
			if (invoiceNo.Length == 0)
			{
				candidate.AddError(rowNumber, L("ImportMissingInvoiceNo"));
			}
			object dateValue = row.Get(DateHeaders);
			DateTime date;
			if (dateValue == null)
			{
				date = Clock.Now.Date;
			}
			else if (!PurchaseImportFileReader.TryParseDate(dateValue, out date))
			{
				candidate.AddError(rowNumber, L("ImportInvalidDate", dateValue));
			}
			if ((dateValue == null || date != default(DateTime)) && !candidate.Preview.InvoiceDate.HasValue)
			{
				candidate.Preview.InvoiceDate = date;
				candidate.Invoice.InvoiceDate = date;
			}
			string supplierText = row.GetText(SupplierHeaders);
			StockPilot.Entity.Supplier supplier = suppliers.FirstOrDefault((StockPilot.Entity.Supplier s) => Same(s.Code, supplierText)) ?? suppliers.FirstOrDefault((StockPilot.Entity.Supplier s) => Same(s.Name, supplierText));
			if (supplier == null)
			{
				candidate.AddError(rowNumber, L("ImportUnknownSupplier", supplierText));
			}
			else if (candidate.Preview.SupplierId.HasValue && candidate.Preview.SupplierId != supplier.Id)
			{
				candidate.AddError(rowNumber, L("ImportDifferentSupplier"));
			}
			else
			{
				candidate.Preview.SupplierId = supplier.Id;
				candidate.Preview.SupplierName = supplier.Name;
				candidate.Invoice.SupplierId = supplier.Id;
			}
			string currencyText = row.GetText(CurrencyHeaders);
			bool flag;
			if (currencyText.Length > 0)
			{
				StockPilot.Entity.Currency currency = currencies.FirstOrDefault((StockPilot.Entity.Currency c) => Same(c.Code, currencyText));
				if (currency == null)
				{
					candidate.AddError(rowNumber, L("ImportUnknownCurrency", currencyText));
				}
				else if (candidate.CurrencyId.HasValue && candidate.CurrencyId != currency.Id)
				{
					candidate.AddError(rowNumber, L("ImportDifferentCurrency"));
				}
				else
				{
					flag = !currency.IsBaseCurrency;
					if (flag)
					{
						flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.MultiCurrency"));
					}
					if (flag)
					{
						candidate.AddError(rowNumber, L("OnlyBaseCurrencyAllowed"));
					}
					else
					{
						candidate.CurrencyId = currency.Id;
					}
				}
			}
			StockPilot.Entity.ItemMaster item = items.FirstOrDefault((StockPilot.Entity.ItemMaster i) => Same(i.ItemCode, itemText)) ?? items.FirstOrDefault((StockPilot.Entity.ItemMaster i) => Same(i.ItemName, itemText));
			if (item == null)
			{
				candidate.AddError(rowNumber, L("ImportUnknownItem", itemText));
				return;
			}
			List<ItemUnitInfo> units = await GetUnitsAsync(unitsByItem, item);
			if (units == null)
			{
				candidate.AddError(rowNumber, L("ImportItemUnitsInvalid", item.ItemName));
				return;
			}
			string unitText = row.GetText(UnitHeaders);
			ItemUnitInfo unit = ((unitText.Length == 0) ? units.FirstOrDefault((ItemUnitInfo u) => u.IsBaseUnit) : units.FirstOrDefault((ItemUnitInfo u) => Same(u.Code, unitText)));
			if (unit == null)
			{
				candidate.AddError(rowNumber, L("ImportUnitNotValid", unitText, item.ItemName));
				return;
			}
			flag = !unit.IsBaseUnit;
			if (flag)
			{
				flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.UnitConversions"));
			}
			if (flag)
			{
				candidate.AddError(rowNumber, L("OnlyBaseUnitAllowed", item.ItemName));
				return;
			}
			if (!PurchaseImportFileReader.TryParseDecimal(row.Get(QuantityHeaders), out var quantity) || quantity <= 0m)
			{
				candidate.AddError(rowNumber, L("ImportQuantityMustBePositive"));
				return;
			}
			object costValue = row.Get(CostHeaders);
			decimal unitCost;
			if (costValue == null)
			{
				unitCost = item.StandardCost * unit.Factor;
			}
			else if (!PurchaseImportFileReader.TryParseDecimal(costValue, out unitCost) || unitCost < 0m)
			{
				candidate.AddError(rowNumber, L("ImportInvalidUnitCost", costValue));
				return;
			}
			candidate.Invoice.Lines.Add(new CreatePurchaseInvoiceItemDto
			{
				ItemMasterId = item.Id,
				UnitMasterId = unit.UnitId,
				Quantity = StockManager.Round(quantity),
				UnitCost = StockManager.Round(unitCost)
			});
		}

		private async Task CompleteAsync(PurchaseImportCandidate candidate, List<StockPilot.Entity.Currency> currencies, StockPilot.Entity.Currency baseCurrency, HashSet<string> existing)
		{
			StockPilot.Entity.Currency currency = (candidate.CurrencyId.HasValue ? currencies.First((StockPilot.Entity.Currency c) => c.Id == candidate.CurrencyId.Value) : baseCurrency);
			if (currency == null)
			{
				candidate.AddError(null, L("ImportCurrencyRequired"));
			}
			else
			{
				candidate.Invoice.CurrencyId = currency.Id;
				candidate.Preview.CurrencyCode = currency.Code;
			}
			if (candidate.Invoice.Lines.Count == 0 && !candidate.HasErrors)
			{
				candidate.AddError(null, L("ImportNoValidLines"));
			}
			if (!candidate.HasErrors && currency != null && candidate.Preview.InvoiceDate.HasValue)
			{
				ExchangeRateResult rate = await _exchangeRateManager.GetRateToBaseOrNullAsync(currency.Id, candidate.Preview.InvoiceDate.Value);
				if (rate == null)
				{
					candidate.AddError(null, L("ImportNoExchangeRate", currency.Code, candidate.Preview.InvoiceDate.Value.ToString("yyyy-MM-dd")));
				}
				else
				{
					candidate.Invoice.ExchangeRate = rate.Rate;
				}
			}
			int decimals = currency?.DecimalPlaces ?? 2;
			candidate.Preview.LineCount = candidate.Invoice.Lines.Count;
			candidate.Preview.Total = candidate.Invoice.Lines.Sum((CreatePurchaseInvoiceItemDto l) => DocumentTotals.Round(l.Quantity * l.UnitCost, decimals));
			if (candidate.HasErrors)
			{
				candidate.Preview.Status = PurchaseImportStatus.Error;
			}
			else if (existing.Contains(candidate.Invoice.InvoiceNo))
			{
				candidate.Preview.Status = PurchaseImportStatus.Exists;
				candidate.Preview.Messages.Add(new PurchaseImportMessageDto
				{
					Message = L("ImportAlreadyExists")
				});
			}
			else
			{
				candidate.Preview.Status = PurchaseImportStatus.Ready;
			}
		}

		private async Task<HashSet<string>> FindExistingNumbersAsync(List<string> numbers)
		{
			HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				for (int i = 0; i < numbers.Count; i += 1000)
				{
					List<string> batch = numbers.Skip(i).Take(1000).ToList();
					found.UnionWith(await (from d in _invoiceRepository.GetAll()
						where batch.Contains(d.InvoiceNo)
						select d.InvoiceNo).ToListAsync());
				}
			}
			return found;
		}

		private async Task<List<ItemUnitInfo>> GetUnitsAsync(Dictionary<int, List<ItemUnitInfo>> cache, StockPilot.Entity.ItemMaster item)
		{
			if (!cache.TryGetValue(item.Id, out var units))
			{
				try
				{
					units = await _unitConversionManager.GetUnitsForItemAsync(item.Id);
				}
				catch (UserFriendlyException)
				{
					units = null;
				}
				cache[item.Id] = units;
			}
			return units;
		}

		private static bool Same(string a, string b)
		{
			if (!string.IsNullOrWhiteSpace(b))
			{
				return string.Equals(a?.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}
}
