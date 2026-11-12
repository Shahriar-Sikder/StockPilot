import { forwardRef, useEffect, useRef, useState } from 'react';
import { callService, L } from '../abp.js';
import Avatar from './avatar.jsx';
import { n } from './format.js';

const MAX = 20; // the server's limit

/**
 * The mockup's "Add items" search (.sgw / .sgl / .sg) on ItemMaster.SearchForDocument.
 *   - focus opens the list straight away (active items by name); typing narrows it, 300ms after typing stops
 *   - blur and Escape close it
 *   - ↑ / ↓ move the highlight, Enter or a click picks it (onPick(item)); the box then clears, keeps focus and
 *     shows the list again so the next item can be picked straight away
 */
const ItemSearch = forwardRef(function ItemSearch({ onPick }, ref) {
  const [text, setText] = useState('');
  const [hits, setHits] = useState(null); // null while loading
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const [reload, setReload] = useState(0); // a pick reloads the list even when the box was already empty
  const list = useRef(null);
  const request = useRef(0);

  // Load when the list is open: at once for an empty box, 300ms after typing stops otherwise.
  useEffect(() => {
    if (!open) return undefined;
    const query = text.trim();
    const id = ++request.current;
    const timer = setTimeout(() => {
      callService('ItemMaster', 'SearchForDocument', { text: query, maxResultCount: MAX })
        .then((result) => { if (id === request.current) { setHits(result.items || []); setActive(0); } })
        .catch(() => { if (id === request.current) setHits([]); });
    }, query ? 300 : 0);
    return () => clearTimeout(timer);
  }, [text, open, reload]);

  // Keep the highlighted suggestion in view while moving with the arrow keys.
  useEffect(() => {
    const row = list.current && list.current.children[active];
    if (row && row.scrollIntoView) row.scrollIntoView({ block: 'nearest' });
  }, [active]);

  const pick = (item) => { setText(''); setHits(null); setActive(0); setReload((k) => k + 1); onPick(item); };

  function onKeyDown(event) {
    const items = hits || [];
    if (event.key === 'ArrowDown') { event.preventDefault(); setOpen(true); setActive((a) => Math.min(a + 1, Math.max(items.length - 1, 0))); }
    else if (event.key === 'ArrowUp') { event.preventDefault(); setActive((a) => Math.max(a - 1, 0)); }
    else if (event.key === 'Enter') { event.preventDefault(); if (open && items[active]) pick(items[active]); }
    else if (event.key === 'Escape' && open) { event.preventDefault(); event.stopPropagation(); setOpen(false); }
  }

  const listId = 'item-search-list';
  return <div className="sgw" style={{ margin: '20px 0 6px' }}>
    <label className="fl">{L('Add items')}
      <input ref={ref} autoComplete="off" placeholder={L('Search by name or code, then press Enter…')} value={text}
        role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list"
        aria-activedescendant={open && hits && hits[active] ? `item-search-${hits[active].id}` : undefined}
        onChange={(event) => { setText(event.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)} onClick={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={onKeyDown} />
    </label>
    {open && <div className="sgl" id={listId} role="listbox" ref={list}>
      {hits === null
        ? <div className="sg-empty mu">{L('Loading…')}</div>
        : hits.length === 0
          ? <div className="sg-empty mu">{text.trim() ? L('No items match "{0}"', text.trim()) : L('No items yet. Add them on the Items page.')}</div>
          : hits.map((item, index) => <div key={item.id} id={`item-search-${item.id}`} role="option" aria-selected={index === active}
            className={`sg${index === active ? ' on' : ''}`}
            onMouseEnter={() => setActive(index)}
            onMouseDown={(event) => { event.preventDefault(); pick(item); }}>
            <Avatar name={item.itemName} picture={{ service: 'ItemMaster', id: item.id, pictureId: item.pictureId }} />
            <div><b>{item.itemName}</b><div className="mu">{item.itemCode}</div></div>
            <span className="mu">{L('{0} in stock', `${n(item.quantityOnHand, 2)} ${item.baseUnitCode}`)}</span>
          </div>)}
    </div>}
  </div>;
});

export default ItemSearch;
