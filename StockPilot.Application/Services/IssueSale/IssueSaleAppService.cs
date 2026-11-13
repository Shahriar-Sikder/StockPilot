using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.IssueSale.Dtos;
using StockPilot.Services.IssueSale.Exporting;

namespace StockPilot.Services.IssueSale
{
	[AbpAuthorize(new string[] { })]
	[RequiresFeature(new string[] { "App.Inventory.Sales" })]
	public class IssueSaleAppService : DocumentAppServiceBase, IIssueSaleAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "documentNo", "DocumentNo" },
			{ "documentDate", "DocumentDate" },
			{ "issueType", "IssueType" },
			{ "customerName", "CustomerName" },
			{ "referenceNo", "ReferenceNo" },
			{ "totalAmount", "TotalAmount" },
			{ "costTotal", "CostTotal" },
			{ "status", "Status" },
			{ "lineCount", "LineCount" },
			{ "postedTime", "PostedTime" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.IssueSale> _documentRepository;

		private readonly IRepository<IssueSalesItem> _lineRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly DocumentNumberManager _documentNumberManager;

		private readonly StockManager _stockManager;

		public IssueSaleExcelExporter ExcelExporter { get; set; }

		public IssueSaleAppService(IRepository<StockPilot.Entity.IssueSale> documentRepository, IRepository<IssueSalesItem> lineRepository, IRepository<StockBalance> balanceRepository, DocumentNumberManager documentNumberManager, StockManager stockManager)
		{
			_documentRepository = documentRepository;
			_lineRepository = lineRepository;
			_balanceRepository = balanceRepository;
			_documentNumberManager = documentNumberManager;
			_stockManager = stockManager;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales" })]
		public async Task<IssueSalePagedResultDto> GetAll(IssueSaleGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<IssueSaleGridRow> baseQuery = FilteredQuery(input);
				var counts = await (from r in baseQuery
					group r by r.Status into g
					select new
					{
						Status = g.Key,
						Count = g.Count()
					}).ToListAsync();
				IQueryable<IssueSaleGridRow> query = baseQuery.WhereIf(input.Status.HasValue, (IssueSaleGridRow r) => (int)r.Status == (int)input.Status.Value);
				int totalCount = await query.CountAsync();
				List<IssueSaleGridRow> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "DocumentDate descending, DocumentNo descending", SortFields).PageBy(input).ToListAsync();
				return new IssueSalePagedResultDto(totalCount, base.ObjectMapper.Map<List<IssueSaleListDto>>(rows))
				{
					AllCount = counts.Sum(c => c.Count),
					DraftCount = counts.Where(c => c.Status == DocumentStatus.Draft).Sum(c => c.Count),
					PostedCount = counts.Where(c => c.Status == DocumentStatus.Posted).Sum(c => c.Count),
					CancelledCount = counts.Where(c => c.Status == DocumentStatus.Cancelled).Sum(c => c.Count)
				};
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(IssueSaleGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<IssueSaleGridRow> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input).WhereIf(input.Status.HasValue, (IssueSaleGridRow r) => (int)r.Status == (int)input.Status.Value), input.Sorting, "DocumentDate descending, DocumentNo descending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<IssueSaleListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales" })]
		public async Task<EditIssueSaleDto> Get(EntityDto input)
		{
			StockPilot.Entity.IssueSale document = await _documentRepository.GetAll().Include((StockPilot.Entity.IssueSale d) => d.CurrencyInfo).FirstOrDefaultAsync((StockPilot.Entity.IssueSale d) => d.Id == input.Id);
			if (document == null)
			{
				throw new EntityNotFoundException(typeof(StockPilot.Entity.IssueSale), input.Id);
			}
			EditIssueSaleDto dto = base.ObjectMapper.Map<EditIssueSaleDto>(document);
			dto.CurrencyCode = document.CurrencyInfo?.Code;
			List<IssueSalesItem> lines;
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				lines = await (from l in _lineRepository.GetAll().Include((IssueSalesItem l) => l.ItemMasterInfo.BaseUnit).Include((IssueSalesItem l) => l.UnitMasterInfo)
					where l.IssueSaleId == document.Id && !l.IsDeleted
					orderby l.Id
					select l).ToListAsync();
			}
			Dictionary<int, decimal> available = null;
			if (document.Status == DocumentStatus.Draft)
			{
				List<int> itemIds = lines.Select((IssueSalesItem l) => l.ItemMasterId).Distinct().ToList();
				available = (await _balanceRepository.GetAllListAsync((StockBalance b) => itemIds.Contains(b.ItemMasterId))).ToDictionary((StockBalance b) => b.ItemMasterId, (StockBalance b) => b.AvailableQuantity);
			}
			dto.Lines = lines.Select(delegate(IssueSalesItem l)
			{
				EditIssueSalesItemDto editIssueSalesItemDto = base.ObjectMapper.Map<EditIssueSalesItemDto>(l);
				editIssueSalesItemDto.ItemCode = l.ItemMasterInfo?.ItemCode;
				editIssueSalesItemDto.ItemName = l.ItemMasterInfo?.ItemName;
				editIssueSalesItemDto.BaseUnitCode = l.ItemMasterInfo?.BaseUnit?.Code;
				editIssueSalesItemDto.UnitCode = l.UnitMasterInfo?.Code;
				if (available != null)
				{
					editIssueSalesItemDto.AvailableQuantity = (available.TryGetValue(l.ItemMasterId, out var value) ? value : 0m);
				}
				return editIssueSalesItemDto;
			}).ToList();
			dto.CostTotal = StockManager.Round(lines.Sum((IssueSalesItem l) => l.BaseQuantity * l.UnitCost));
			return dto;
		}

		public async Task<ListResultDto<string>> GetCustomers(GetCustomersInput input)
		{
			string filter = input.Filter?.Trim();
			return new ListResultDto<string>(await (from n in (from d in (from d in _documentRepository.GetAll()
						where d.CustomerName != null && d.CustomerName != ""
						select d).WhereIf(!filter.IsNullOrEmpty(), (StockPilot.Entity.IssueSale d) => d.CustomerName.Contains(filter))
					select d.CustomerName.Trim()).Distinct()
				orderby n
				select n).Take(input.MaxResultCount).ToListAsync());
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales.Create" })]
		public async Task<EntityDto> Create(CreateIssueSaleDto input)
		{
			StockPilot.Entity.Currency currency = await GetCurrencyAsync(input.CurrencyId);
			StockPilot.Entity.IssueSale document = MapForCreate<StockPilot.Entity.IssueSale>(input);
			document.DocumentDate = input.DocumentDate.Date;
			document.CustomerName = input.CustomerName?.Trim();
			StockPilot.Entity.IssueSale issueSale = document;
			issueSale.DocumentNo = await _documentNumberManager.GetNextNumberAsync(DocumentNumberType.IssueSale);
			issueSale = document;
			issueSale.ExchangeRate = await ResolveExchangeRateAsync(currency, input.ExchangeRate, document.DocumentDate);
			ResetPosting(document);
			Dictionary<int, StockPilot.Entity.ItemMaster> items = new Dictionary<int, StockPilot.Entity.ItemMaster>();
			List<IssueSalesItem> lines = new List<IssueSalesItem>();
			foreach (CreateIssueSalesItemDto lineInput in input.Lines ?? new List<CreateIssueSalesItemDto>())
			{
				IssueSalesItem line = MapForCreate<IssueSalesItem>(lineInput);
				await PrepareLineAsync(line, items, currency);
				lines.Add(line);
			}
			DocumentTotals.CalculateHeader(document, lines, currency.DecimalPlaces);
			int id = await _documentRepository.InsertAndGetIdAsync(document);
			foreach (IssueSalesItem line2 in lines)
			{
				line2.IssueSaleId = id;
				await _lineRepository.InsertAsync(line2);
			}
			return new EntityDto(id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales.Edit" })]
		public async Task Update(EditIssueSaleDto input)
		{
			StockPilot.Entity.IssueSale document = await _documentRepository.GetAsync(input.Id);
			EnsureDraft(document.Status, document.DocumentNo);
			StockPilot.Entity.Currency currency = await GetCurrencyAsync(input.CurrencyId);
			string documentNo = document.DocumentNo;
			MapForUpdate(input, document);
			document.DocumentNo = documentNo;
			document.DocumentDate = input.DocumentDate.Date;
			document.CustomerName = input.CustomerName?.Trim();
			StockPilot.Entity.IssueSale issueSale = document;
			issueSale.ExchangeRate = await ResolveExchangeRateAsync(currency, input.ExchangeRate, document.DocumentDate);
			ResetPosting(document);
			List<IssueSalesItem> existing = await _lineRepository.GetAllListAsync((IssueSalesItem l) => l.IssueSaleId == document.Id);
			List<EditIssueSalesItemDto> inputLines = input.Lines ?? new List<EditIssueSalesItemDto>();
			HashSet<int> keptIds = new HashSet<int>(from l in inputLines
				where l.Id > 0
				select l.Id);
			foreach (IssueSalesItem removed in existing.Where((IssueSalesItem l) => !keptIds.Contains(l.Id)))
			{
				await _lineRepository.DeleteAsync(removed);
			}
			Dictionary<int, StockPilot.Entity.ItemMaster> items = new Dictionary<int, StockPilot.Entity.ItemMaster>();
			List<IssueSalesItem> lines = new List<IssueSalesItem>();
			foreach (EditIssueSalesItemDto lineInput in inputLines)
			{
				IssueSalesItem line;
				if (lineInput.Id > 0)
				{
					line = existing.FirstOrDefault((IssueSalesItem l) => l.Id == lineInput.Id);
					if (line == null)
					{
						throw new UserFriendlyException(L("DocumentLineNotFound"));
					}
					MapForUpdate(lineInput, line);
				}
				else
				{
					line = MapForCreate<IssueSalesItem>(lineInput);
					await _lineRepository.InsertAsync(line);
				}
				line.IssueSaleId = document.Id;
				await PrepareLineAsync(line, items, currency);
				lines.Add(line);
			}
			DocumentTotals.CalculateHeader(document, lines, currency.DecimalPlaces);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales.Delete" })]
		public async Task DeleteDraft(EntityDto input)
		{
			StockPilot.Entity.IssueSale document = await _documentRepository.GetAsync(input.Id);
			EnsureDraft(document.Status, document.DocumentNo);
			foreach (IssueSalesItem line in await _lineRepository.GetAllListAsync((IssueSalesItem l) => l.IssueSaleId == document.Id))
			{
				await _lineRepository.DeleteAsync(line);
			}
			await _documentRepository.DeleteAsync(document);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales.Post" })]
		public async Task Post(EntityDto input)
		{
			await _stockManager.PostIssueSaleAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Sales.Cancel" })]
		public async Task Cancel(EntityDto input)
		{
			await _stockManager.CancelIssueSaleAsync(input.Id);
		}

		private IQueryable<IssueSaleGridRow> FilteredQuery(IssueSaleGridInput input)
		{
			DateTime? dateFrom = input.DateFrom?.Date;
			DateTime? dateToExclusive = DocumentAppServiceBase.EndOfDayExclusive(input.DateTo);
			string customer = input.CustomerName?.Trim();
			return BuildGridQuery().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (IssueSaleGridRow r) => r.DocumentNo.Contains(input.Filter) || r.ReferenceNo.Contains(input.Filter) || r.CustomerName.Contains(input.Filter)).WhereIf(dateFrom.HasValue, (IssueSaleGridRow r) => r.DocumentDate >= ((DateTime?)dateFrom).Value).WhereIf(dateToExclusive.HasValue, (IssueSaleGridRow r) => r.DocumentDate < ((DateTime?)dateToExclusive).Value)
				.WhereIf(input.IssueType.HasValue, (IssueSaleGridRow r) => (int)r.IssueType == (int)input.IssueType.Value)
				.WhereIf(!customer.IsNullOrEmpty(), (IssueSaleGridRow r) => r.CustomerName == customer);
		}

		private IQueryable<IssueSaleGridRow> BuildGridQuery()
		{
			return from d in _documentRepository.GetAll()
				select new IssueSaleGridRow
				{
					Id = d.Id,
					DocumentNo = d.DocumentNo,
					DocumentDate = d.DocumentDate,
					IssueType = d.IssueType,
					CustomerName = d.CustomerName,
					ReferenceNo = d.ReferenceNo,
					CurrencyCode = d.CurrencyInfo.Code,
					SubTotal = d.SubTotal,
					DiscountAmount = d.DiscountAmount,
					TaxAmount = d.TaxAmount,
					TotalAmount = d.TotalAmount,
					CostTotal = (d.Items.Where((IssueSalesItem l) => !l.IsDeleted).Sum((IssueSalesItem l) => (decimal?)(l.BaseQuantity * l.UnitCost)) ?? 0m),
					Status = d.Status,
					PostedTime = d.PostedTime,
					LineCount = d.Items.Count((IssueSalesItem l) => !l.IsDeleted),
					IsDeleted = d.IsDeleted,
					CreationTime = d.CreationTime
				};
		}

		private async Task PrepareLineAsync(IssueSalesItem line, Dictionary<int, StockPilot.Entity.ItemMaster> items, StockPilot.Entity.Currency currency)
		{
			StockPilot.Entity.ItemMaster item = await GetItemAsync(items, line.ItemMasterId);
			if (line.Quantity <= 0m)
			{
				throw new UserFriendlyException(L("LineQuantityMustBePositive", item.ItemName));
			}
			line.ConversionFactor = await GetFactorAsync(item, line.UnitMasterId);
			line.BaseQuantity = StockManager.Round(line.Quantity * line.ConversionFactor);
			line.UnitCost = 0m;
			DocumentTotals.CalculateLine(line, currency.DecimalPlaces);
			if (!DocumentTotals.IsDiscountValid(line.Quantity * line.UnitPrice, line.DiscountAmount))
			{
				throw new UserFriendlyException(L("LineValueCannotBeNegative", item.ItemName));
			}
		}

		private static void ResetPosting(StockPilot.Entity.IssueSale document)
		{
			document.Status = DocumentStatus.Draft;
			document.IsPosted = false;
			document.PostedBy = null;
			document.PostedTime = null;
		}
	}
}
