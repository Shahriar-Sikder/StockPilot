using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.UI;
using StockPilot.Entity;
using StockPilot.Inventory;

namespace StockPilot.Services.Common
{
	public abstract class DocumentAppServiceBase : MasterDataAppServiceBase
	{
		public IRepository<StockPilot.Entity.Currency> CurrencyRepository { get; set; }

		public IRepository<StockPilot.Entity.ItemMaster> ItemRepository { get; set; }

		public IRepository<StockPilot.Entity.UnitMaster> UnitRepository { get; set; }

		public ExchangeRateManager ExchangeRateManager { get; set; }

		public UnitConversionManager UnitConversionManager { get; set; }

		protected async Task<StockPilot.Entity.Currency> GetCurrencyAsync(int currencyId)
		{
			StockPilot.Entity.Currency currency = await CurrencyRepository.FirstOrDefaultAsync(currencyId);
			if (currency == null)
			{
				throw new UserFriendlyException(L("CurrencyNotFound"));
			}
			bool flag = !currency.IsBaseCurrency;
			if (flag)
			{
				flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.MultiCurrency"));
			}
			if (flag)
			{
				throw new UserFriendlyException(L("OnlyBaseCurrencyAllowed"));
			}
			return currency;
		}

		protected async Task<decimal> ResolveExchangeRateAsync(StockPilot.Entity.Currency currency, decimal givenRate, DateTime documentDate)
		{
			if (givenRate > 0m)
			{
				return givenRate;
			}
			ExchangeRateResult rate = await ExchangeRateManager.GetRateToBaseOrNullAsync(currency.Id, documentDate);
			if (rate == null)
			{
				StockPilot.Entity.Currency baseCurrency = await CurrencyRepository.FirstOrDefaultAsync((StockPilot.Entity.Currency c) => c.IsBaseCurrency);
				throw new UserFriendlyException(L("NoExchangeRateFound", currency.Code, baseCurrency?.Code ?? "?", documentDate.ToString("yyyy-MM-dd")));
			}
			return rate.Rate;
		}

		protected async Task<StockPilot.Entity.ItemMaster> GetItemAsync(Dictionary<int, StockPilot.Entity.ItemMaster> cache, int itemId)
		{
			if (!cache.TryGetValue(itemId, out var item))
			{
				item = await ItemRepository.FirstOrDefaultAsync(itemId);
				if (item == null)
				{
					throw new UserFriendlyException(L("ItemNotFound"));
				}
				cache[itemId] = item;
			}
			return item;
		}

		protected async Task<decimal> GetFactorAsync(StockPilot.Entity.ItemMaster item, int unitId)
		{
			bool flag = unitId != item.BaseUnitId;
			if (flag)
			{
				flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.UnitConversions"));
			}
			if (flag)
			{
				throw new UserFriendlyException(L("OnlyBaseUnitAllowed", item.ItemName));
			}
			return await UnitConversionManager.GetFactorAsync(item, unitId);
		}

		protected void EnsureDraft(DocumentStatus status, string documentNo)
		{
			if (status != DocumentStatus.Draft)
			{
				throw new UserFriendlyException(L("OnlyDraftCanBeChanged", documentNo));
			}
		}

		protected static DateTime? EndOfDayExclusive(DateTime? date)
		{
			return date?.Date.AddDays(1.0);
		}
	}
}
