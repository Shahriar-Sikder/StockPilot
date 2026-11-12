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
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.CurrencyConversion.Dtos;
using StockPilot.Services.CurrencyConversion.Exporting;

namespace StockPilot.Services.CurrencyConversion
{
	[AbpAuthorize(new string[] { })]
	public class CurrencyConversionAppService : MasterDataAppServiceBase, ICurrencyConversionAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "fromCurrencyCode", "FromCurrency.Code" },
			{ "toCurrencyCode", "ToCurrency.Code" },
			{ "rate", "Rate" },
			{ "effectiveDate", "EffectiveDate" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.CurrencyConversion> _conversionRepository;

		private readonly IRepository<StockPilot.Entity.Currency> _currencyRepository;

		private readonly ExchangeRateManager _exchangeRateManager;

		public CurrencyConversionExcelExporter ExcelExporter { get; set; }

		public CurrencyConversionAppService(IRepository<StockPilot.Entity.CurrencyConversion> conversionRepository, IRepository<StockPilot.Entity.Currency> currencyRepository, ExchangeRateManager exchangeRateManager)
		{
			_conversionRepository = conversionRepository;
			_currencyRepository = currencyRepository;
			_exchangeRateManager = exchangeRateManager;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions" })]
		public async Task<PagedResultDto<CurrencyConversionListDto>> GetAll(CurrencyConversionGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.CurrencyConversion> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.CurrencyConversion> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "EffectiveDate descending, FromCurrency.Code ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<CurrencyConversionListDto>(totalCount, base.ObjectMapper.Map<List<CurrencyConversionListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(CurrencyConversionGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.CurrencyConversion> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "EffectiveDate descending, FromCurrency.Code ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<CurrencyConversionListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions" })]
		public async Task<EditCurrencyConversionDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditCurrencyConversionDto>(await _conversionRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions.Create" })]
		[RequiresFeature(new string[] { "App.Inventory.MultiCurrency" })]
		public async Task<EntityDto> Create(CreateCurrencyConversionDto input)
		{
			input.EffectiveDate = input.EffectiveDate.Date;
			await ValidateAsync(input.FromCurrencyId, input.ToCurrencyId, input.Rate, input.EffectiveDate, null);
			return new EntityDto(await _conversionRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.CurrencyConversion>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.MultiCurrency" })]
		public async Task Update(EditCurrencyConversionDto input)
		{
			input.EffectiveDate = input.EffectiveDate.Date;
			StockPilot.Entity.CurrencyConversion entity = await _conversionRepository.GetAsync(input.Id);
			await ValidateAsync(input.FromCurrencyId, input.ToCurrencyId, input.Rate, input.EffectiveDate, entity.Id);
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions.Delete" })]
		[RequiresFeature(new string[] { "App.Inventory.MultiCurrency" })]
		public async Task Delete(EntityDto input)
		{
			await _conversionRepository.DeleteAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.CurrencyConversions.Delete" })]
		[RequiresFeature(new string[] { "App.Inventory.MultiCurrency" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_conversionRepository, input.Id, (StockPilot.Entity.CurrencyConversion entity) => EnsureNoDuplicateAsync(entity.FromCurrencyId, entity.ToCurrencyId, entity.EffectiveDate, entity.Id));
		}

		public async Task<ListResultDto<CurrencyConversionComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.CurrencyConversion> rows = await (from c in _conversionRepository.GetAll().Include((StockPilot.Entity.CurrencyConversion c) => c.FromCurrency).Include((StockPilot.Entity.CurrencyConversion c) => c.ToCurrency)
				where c.IsActive
				orderby c.EffectiveDate descending, c.FromCurrency.Code
				select c).ToListAsync();
			return new ListResultDto<CurrencyConversionComboDto>(base.ObjectMapper.Map<List<CurrencyConversionComboDto>>(rows));
		}

		public async Task<CurrencyRateDto> GetRate(GetCurrencyRateInput input)
		{
			ExchangeRateResult result = await _exchangeRateManager.GetRateOrNullAsync(input.FromCurrencyId, input.ToCurrencyId, input.Date);
			if (result != null)
			{
				return new CurrencyRateDto
				{
					Rate = result.Rate,
					EffectiveDate = result.EffectiveDate,
					IsInverse = result.IsInverse
				};
			}
			StockPilot.Entity.Currency from = await _currencyRepository.FirstOrDefaultAsync(input.FromCurrencyId);
			StockPilot.Entity.Currency to = await _currencyRepository.FirstOrDefaultAsync(input.ToCurrencyId);
			throw new UserFriendlyException(L("NoExchangeRateFound", from?.Code ?? "?", to?.Code ?? "?", input.Date.ToString("yyyy-MM-dd")));
		}

		private async Task ValidateAsync(int fromCurrencyId, int toCurrencyId, decimal rate, DateTime effectiveDate, int? exceptId)
		{
			if (fromCurrencyId == toCurrencyId)
			{
				throw new UserFriendlyException(L("CurrencyConversionSameCurrency"));
			}
			if (rate <= 0m)
			{
				throw new UserFriendlyException(L("RateMustBePositive"));
			}
			bool flag = await _currencyRepository.FirstOrDefaultAsync(fromCurrencyId) == null;
			if (!flag)
			{
				flag = await _currencyRepository.FirstOrDefaultAsync(toCurrencyId) == null;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("CurrencyNotFound"));
			}
			await EnsureNoDuplicateAsync(fromCurrencyId, toCurrencyId, effectiveDate, exceptId);
		}

		private async Task EnsureNoDuplicateAsync(int fromCurrencyId, int toCurrencyId, DateTime effectiveDate, int? exceptId)
		{
			DateTime day = effectiveDate.Date;
			DateTime dayAfter = day.AddDays(1.0);
			if (await _conversionRepository.CountAsync((StockPilot.Entity.CurrencyConversion c) => c.FromCurrencyId == fromCurrencyId && c.ToCurrencyId == toCurrencyId && c.EffectiveDate >= day && c.EffectiveDate < dayAfter && (int?)c.Id != exceptId) > 0)
			{
				throw new UserFriendlyException(L("CurrencyRateAlreadyExistsForDate", day.ToString("yyyy-MM-dd")));
			}
		}

		private IQueryable<StockPilot.Entity.CurrencyConversion> FilteredQuery(CurrencyConversionGridInput input)
		{
			DateTime? effectiveToExclusive = input.EffectiveTo?.Date.AddDays(1.0);
			return _conversionRepository.GetAll().Include((StockPilot.Entity.CurrencyConversion c) => c.FromCurrency).Include((StockPilot.Entity.CurrencyConversion c) => c.ToCurrency)
				.WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.CurrencyConversion c) => c.FromCurrency.Code.Contains(input.Filter) || c.ToCurrency.Code.Contains(input.Filter))
				.WhereIf(input.FromCurrencyId.HasValue, (StockPilot.Entity.CurrencyConversion c) => c.FromCurrencyId == input.FromCurrencyId.Value)
				.WhereIf(input.ToCurrencyId.HasValue, (StockPilot.Entity.CurrencyConversion c) => c.ToCurrencyId == input.ToCurrencyId.Value)
				.WhereIf(input.EffectiveFrom.HasValue, (StockPilot.Entity.CurrencyConversion c) => c.EffectiveDate >= input.EffectiveFrom.Value)
				.WhereIf(effectiveToExclusive.HasValue, (StockPilot.Entity.CurrencyConversion c) => c.EffectiveDate < ((DateTime?)effectiveToExclusive).Value)
				.WhereIf(input.IsActive.HasValue, (StockPilot.Entity.CurrencyConversion c) => c.IsActive == input.IsActive.Value);
		}
	}
}
