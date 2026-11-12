using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Services.Common;
using StockPilot.Services.Currency.Dtos;
using StockPilot.Services.Currency.Exporting;

namespace StockPilot.Services.Currency
{
	[AbpAuthorize(new string[] { })]
	public class CurrencyAppService : MasterDataAppServiceBase, ICurrencyAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "symbol", "Symbol" },
			{ "decimalPlaces", "DecimalPlaces" },
			{ "isBaseCurrency", "IsBaseCurrency" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.Currency> _currencyRepository;

		private readonly IRepository<StockPilot.Entity.CurrencyConversion> _conversionRepository;

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<StockPilot.Entity.IssueSale> _issueSaleRepository;

		public CurrencyExcelExporter ExcelExporter { get; set; }

		public CurrencyAppService(IRepository<StockPilot.Entity.Currency> currencyRepository, IRepository<StockPilot.Entity.CurrencyConversion> conversionRepository, IRepository<StockPilot.Entity.Supplier> supplierRepository, IRepository<StockPilot.Entity.PurchaseInvoice> purchaseInvoiceRepository, IRepository<StockPilot.Entity.IssueSale> issueSaleRepository)
		{
			_currencyRepository = currencyRepository;
			_conversionRepository = conversionRepository;
			_supplierRepository = supplierRepository;
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_issueSaleRepository = issueSaleRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies" })]
		public async Task<PagedResultDto<CurrencyListDto>> GetAll(CurrencyGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.Currency> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.Currency> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Sequence ascending, Code ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<CurrencyListDto>(totalCount, base.ObjectMapper.Map<List<CurrencyListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(CurrencyGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.Currency> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Sequence ascending, Code ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<CurrencyListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies" })]
		public async Task<EditCurrencyDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditCurrencyDto>(await _currencyRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies.Create" })]
		public async Task<EntityDto> Create(CreateCurrencyDto input)
		{
			input.Code = input.Code.Trim();
			await CheckMultiCurrencyAsync(input.IsBaseCurrency);
			await EnsureCodeIsFreeAsync(input.Code, null);
			if (input.IsBaseCurrency)
			{
				await EnsureNoOtherBaseCurrencyAsync(null);
			}
			return new EntityDto(await _currencyRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.Currency>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies.Edit" })]
		public async Task Update(EditCurrencyDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.Currency entity = await _currencyRepository.GetAsync(input.Id);
			await CheckMultiCurrencyAsync(input.IsBaseCurrency && entity.IsBaseCurrency);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			if (input.IsBaseCurrency)
			{
				await EnsureNoOtherBaseCurrencyAsync(entity.Id);
			}
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.Currency entity = await _currencyRepository.GetAsync(input.Id);
			if (entity.IsBaseCurrency)
			{
				throw new UserFriendlyException(L("BaseCurrencyCannotBeDeleted", entity.Code));
			}
			await CheckMultiCurrencyAsync(isBaseCurrency: false);
			int id = entity.Id;
			if (await _conversionRepository.CountAsync((StockPilot.Entity.CurrencyConversion c) => c.FromCurrencyId == id || c.ToCurrencyId == id) > 0)
			{
				throw InUse(entity.Code, L("CurrencyConversions"));
			}
			if (await _supplierRepository.CountAsync((StockPilot.Entity.Supplier s) => s.CurrencyId == (int?)id) > 0)
			{
				throw InUse(entity.Code, L("Suppliers"));
			}
			bool flag = await _purchaseInvoiceRepository.CountAsync((StockPilot.Entity.PurchaseInvoice d) => d.CurrencyId == id) > 0;
			if (!flag)
			{
				flag = await _issueSaleRepository.CountAsync((StockPilot.Entity.IssueSale d) => d.CurrencyId == id) > 0;
			}
			if (flag)
			{
				throw InUse(entity.Code, L("Documents"));
			}
			await _currencyRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Currencies.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_currencyRepository, input.Id, async delegate(StockPilot.Entity.Currency entity)
			{
				await CheckMultiCurrencyAsync(entity.IsBaseCurrency);
				await EnsureCodeIsFreeAsync(entity.Code, entity.Id);
			});
		}

		private async Task CheckMultiCurrencyAsync(bool isBaseCurrency)
		{
			bool flag = !isBaseCurrency;
			if (flag)
			{
				flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.MultiCurrency"));
			}
			if (flag)
			{
				throw new UserFriendlyException(L("FeatureNotEnabledForStore"));
			}
		}

		public async Task<ListResultDto<CurrencyComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.Currency> rows = await (from c in _currencyRepository.GetAll()
				where c.IsActive
				orderby c.IsBaseCurrency descending, c.Sequence, c.Code
				select c).ToListAsync();
			return new ListResultDto<CurrencyComboDto>(base.ObjectMapper.Map<List<CurrencyComboDto>>(rows));
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _currencyRepository.CountAsync((StockPilot.Entity.Currency c) => c.Code == code && (int?)c.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private async Task EnsureNoOtherBaseCurrencyAsync(int? exceptId)
		{
			StockPilot.Entity.Currency existing = await _currencyRepository.FirstOrDefaultAsync((StockPilot.Entity.Currency c) => c.IsBaseCurrency && (int?)c.Id != exceptId);
			if (existing != null)
			{
				throw new UserFriendlyException(L("BaseCurrencyAlreadyExists", existing.Code));
			}
		}

		private IQueryable<StockPilot.Entity.Currency> FilteredQuery(CurrencyGridInput input)
		{
			return _currencyRepository.GetAll().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.Currency c) => c.Code.Contains(input.Filter) || c.Name.Contains(input.Filter) || c.Symbol.Contains(input.Filter)).WhereIf(input.IsActive.HasValue, (StockPilot.Entity.Currency c) => c.IsActive == input.IsActive.Value);
		}
	}
}
