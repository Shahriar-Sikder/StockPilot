import { forwardRef, useEffect, useRef, useState } from 'react';
import { callService, L } from '../abp.js';
import Avatar from './avatar.jsx';
import { n } from './format.js';
import { isEscapeKey } from './modal.jsx';

const MAX = 20; // the server's limit

/**
 * The mockup's "Add items" search (.sgw / .sgl / .sg) on ItemMaster.SearchForDocument.
 *   - the list shows by default whenever the box has focus (the editor focuses it when it opens and after each
 *     pick), so it is there straight away
 *   - a click on the box (while it already has focus) hides the list; another click shows it again
 *   - typing shows it again and narrows it, 300ms after typing stops; ↓ shows it too
 *   - Escape hides it (a second Escape reaches the modal and closes it); a click anywhere else (blur) closes it,
 *     and the next focus shows it again by default
 *   - ↑ / ↓ move the highlight, Enter or a click picks it (onPick(item)); the box then clears, keeps focus and
 *     shows the list again so the next item can be picked straight away
 */
const ItemSearch = forwardRef(function ItemSearch({ onPick }, ref) {
  const [text, setText] = useState('');
  const [hits, setHits] = useState(null); // null while loading
  const [focused, setFocused] = useState(false);
  const [hidden, setHidden] = useState(false); // hidden by a click or Escape until shown again
  const [active, setActive] = useState(0);
  const [reload, setReload] = useState(0); // a pick reloads the list even when the box was already empty
  const list = useRef(null);
  const request = useRef(0);
  const hadFocus = useRef(false); // whether the box had focus before this click (the first click only focuses)
  const open = focused && !hidden;

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

  // Escape hides the open list. Listened for on the document (capture), like the modal, so it works wherever the
  // key arrives; the modal sees the list still open on this same key and stays open (modal.jsx).
  useEffect(() => {
    if (!open) return undefined;
    const key = (event) => { if (isEscapeKey(event)) { event.preventDefault(); setHidden(true); } };
    document.addEventListener('keydown', key, true);
    return () => document.removeEventListener('keydown', key, true);
  }, [open]);

  // Keep the highlighted suggestion in view while moving with the arrow keys.
  useEffect(() => {
    const row = list.current && list.current.children[active];
    if (row && row.scrollIntoView) row.scrollIntoView({ block: 'nearest' });
  }, [active]);

  const pick = (item) => { setText(''); setHits(null); setActive(0); setHidden(false); setReload((k) => k + 1); onPick(item); };

  function onKeyDown(event) {
    const items = hits || [];
    if (event.key === 'ArrowDown') { event.preventDefault(); setHidden(false); setActive((a) => Math.min(a + 1, Math.max(items.length - 1, 0))); }
    else if (event.key === 'ArrowUp') { event.preventDefault(); setActive((a) => Math.max(a - 1, 0)); }
    else if (event.key === 'Enter') { event.preventDefault(); if (open && items[active]) pick(items[active]); }
  }

  const listId = 'item-search-list';
  return <div className="sgw" style={{ margin: '20px 0 6px' }}>
    <label className="fl">{L('Add items')}
      <input ref={ref} autoComplete="off" placeholder={L('Search by name or code, then press Enter…')} value={text}
        role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list"
        aria-activedescendant={open && hits && hits[active] ? `item-search-${hits[active].id}` : undefined}
        onChange={(event) => { setText(event.target.value); setHidden(false); }}
        onMouseDown={(event) => { hadFocus.current = document.activeElement === event.currentTarget; }}
        onClick={() => { if (hadFocus.current) setHidden((isHidden) => !isHidden); }}
        onFocus={() => setFocused(true)}
        onBlur={() => { setFocused(false); setHidden(false); }}
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
            <Avatar name={item.itemName} src={item.pictureUrl} />
            <div><b>{item.itemName}</b><div className="mu">{item.itemCode}</div></div>
            <span className="mu">{L('{0} in stock', `${n(item.quantityOnHand, 2)} ${item.baseUnitCode}`)}</span>
          </div>)}
    </div>}
  </div>;
});

export default ItemSearch;
