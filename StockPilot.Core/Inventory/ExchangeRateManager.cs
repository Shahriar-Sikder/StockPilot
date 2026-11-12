using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class ExchangeRateManager : StockPilotDomainServiceBase
	{
		private readonly IRepository<CurrencyConversion> _conversionRepository;

		private readonly IRepository<Currency> _currencyRepository;

		public ExchangeRateManager(IRepository<CurrencyConversion> conversionRepository, IRepository<Currency> currencyRepository)
		{
			_conversionRepository = conversionRepository;
			_currencyRepository = currencyRepository;
		}

		[UnitOfWork]
		public virtual async Task<ExchangeRateResult> GetRateOrNullAsync(int fromCurrencyId, int toCurrencyId, DateTime date)
		{
			if (fromCurrencyId == toCurrencyId)
			{
				return new ExchangeRateResult
				{
					Rate = 1m
				};
			}
			DateTime dayAfter = date.Date.AddDays(1.0);
			CurrencyConversion direct = (from c in await _conversionRepository.GetAllListAsync((CurrencyConversion c) => c.IsActive && c.FromCurrencyId == fromCurrencyId && c.ToCurrencyId == toCurrencyId && c.EffectiveDate < dayAfter)
				orderby c.EffectiveDate descending, c.Id descending
				select c).FirstOrDefault();
			if (direct != null)
			{
				return new ExchangeRateResult
				{
					Rate = direct.Rate,
					EffectiveDate = direct.EffectiveDate
				};
			}
			CurrencyConversion inverse = (from c in await _conversionRepository.GetAllListAsync((CurrencyConversion c) => c.IsActive && c.FromCurrencyId == toCurrencyId && c.ToCurrencyId == fromCurrencyId && c.EffectiveDate < dayAfter && c.Rate > 0m)
				orderby c.EffectiveDate descending, c.Id descending
				select c).FirstOrDefault();
			if (inverse != null)
			{
				return new ExchangeRateResult
				{
					Rate = Math.Round(1m / inverse.Rate, 6),
					EffectiveDate = inverse.EffectiveDate,
					IsInverse = true
				};
			}
			return null;
		}

		[UnitOfWork]
		public virtual async Task<ExchangeRateResult> GetRateToBaseOrNullAsync(int currencyId, DateTime date)
		{
			Currency baseCurrency = await _currencyRepository.FirstOrDefaultAsync((Currency c) => c.IsBaseCurrency);
			if (baseCurrency == null)
			{
				return new ExchangeRateResult
				{
					Rate = 1m
				};
			}
			return await GetRateOrNullAsync(currencyId, baseCurrency.Id, date);
		}
	}
}
