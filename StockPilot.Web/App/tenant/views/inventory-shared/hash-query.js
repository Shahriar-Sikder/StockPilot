import { useEffect, useRef } from 'react';

/**
 * Runs handler(URLSearchParams) when the page's hash carries a query (#/items?open=12, #/suppliers?new=1),
 * on mount and on later hash changes, then drops the query from the address so a reload does not repeat it.
 * Used by the New menu, the palette and links between pages.
 */
export function useHashQuery(handler) {
  const latest = useRef(handler);
  latest.current = handler;
  useEffect(() => {
    const check = () => {
      const [path, query] = window.location.hash.slice(1).split('?');
      if (!query) return;
      history.replaceState(null, '', `#${path}`);
      latest.current(new URLSearchParams(query));
    };
    check();
    window.addEventListener('hashchange', check);
    return () => window.removeEventListener('hashchange', check);
  }, []);
}
