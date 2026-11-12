import { useEffect, useState } from 'react';
import { callService } from '../abp.js';

// The tenant's base currency (Currency.GetForCombo, IsBaseCurrency), loaded once per page load.
// The mockup writes money as "৳ 1,234"; the symbol here comes from the server instead.
let loading = null;

export function loadBaseCurrency() {
  if (!loading) {
    loading = callService('Currency', 'GetForCombo')
      .then((result) => (result.items || []).find((currency) => currency.isBaseCurrency) || null)
      .catch(() => null);
  }
  return loading;
}

/** Symbol (or code) of the base currency; '' until it has loaded or when there is none. */
export function useBaseSymbol() {
  const [symbol, setSymbol] = useState('');
  useEffect(() => {
    let live = true;
    loadBaseCurrency().then((currency) => { if (live && currency) setSymbol(currency.symbol || currency.code || ''); });
    return () => { live = false; };
  }, []);
  return symbol;
}

/** "৳ 1,234" — the mockup's money format; without a symbol just the number. */
export const money = (symbol, text) => (symbol ? `${symbol} ${text}` : text);
