import { useEffect, useRef, useState } from 'react';
import { callService, clearBusy, isGranted, L, notifyError, notifySuccess, setBusy } from '../abp.js';
import Avatar from './avatar.jsx';
import { loadBaseCurrency } from './currency.js';
import { day, fileSize, n } from './format.js';
import Icon from './icons.jsx';
import { FEATURES, isFeatureEnabled } from './features.js';
import ItemSearch from './item-search.jsx';
import { isCostHidden } from './cost-visibility.js';
import Modal from './modal.jsx';

// Attachments: the mockup's FTA list, checked again by the server (PurchaseDocumentRules), at most 3 MB each.
export const DOCUMENT_TYPES = '.pdf,.png,.jpg,.jpeg,.webp,.docx,.xlsx,.csv,.txt';
const DOCUMENT_TYPE = /\.(pdf|png|jpe?g|webp|docx|xlsx|csv|txt)$/i;
export const MAX_DOCUMENT_BYTES = 3 * 1024 * 1024;

/** Reads files picked for upload as base64, rejecting unsupported types and files over 3 MB with the mockup's toasts. */
export function readDocuments(fileList) {
  return Promise.all([...fileList].map((file) => {
    if (!DOCUMENT_TYPE.test(file.name)) { notifyError(L('{0}: file type not supported', file.name)); return null; }
    if (file.size > MAX_DOCUMENT_BYTES) { notifyError(L('{0} is larger than 3 MB', file.name)); return null; }
    return new Promise((resolve) => {
      const reader = new FileReader();
      reader.onload = () => resolve({ name: file.name, size: file.size, base64: String(reader.result).split(',')[1] });
      reader.onerror = () => { notifyError(L('{0}: file could not be read', file.name)); resolve(null); };
      reader.readAsDataURL(file);
    });
  })).then((files) => files.filter(Boolean));
}

// IssueSaleType values with the mockup's wording.
export const ISSUE_TYPES = [[1, 'Sale'], [2, 'Internal use'], [3, 'Damaged'], [4, 'Sample']];

/**
 * Per document kind: service, look, default line price, how header + lines become the Create input, and how the
 * document drawer reads the Get result (number, date, party, line price, stock sign).
 */
export const KINDS = {
  purchase: {
    service: 'PurchaseInvoice', icon: '↓', colour: 'var(--ok)', title: 'Receive stock', sub: 'Record a purchase from a supplier',
    priceTitle: 'Unit cost', basePrice: (item) => item.standardCost, attachments: true, canImport: true, party: 'supplier',
    postPermission: 'Pages.StockPilot.Purchases.Post', cancelPermission: 'Pages.StockPilot.Purchases.Cancel', attachPermission: 'Pages.StockPilot.Purchases.Edit',
    toInput: (head, lines) => ({
      invoiceDate: head.date, supplierId: head.partyId, currencyId: head.currencyId, exchangeRate: 0,
      lines: lines.map((l) => ({ itemMasterId: l.itemId, unitMasterId: l.unitId, quantity: Number(l.qty), unitCost: Number(l.price) }))
    }),
    numberOf: (doc) => doc.invoiceNo, dateOf: (doc) => doc.invoiceDate, partyLabel: 'Supplier', partyOf: (doc) => doc.supplierName,
    docLabel: 'Purchase', sign: '+', linePrice: (line) => line.unitCost
  },
  sale: {
    service: 'IssueSale', icon: '↑', colour: 'var(--in)', title: 'Sell or issue stock', sub: 'Record items leaving your stock',
    priceTitle: 'Unit price', basePrice: (item) => item.salePrice, attachments: false, canImport: false, party: 'customer', checkStock: true,
    postPermission: 'Pages.StockPilot.Sales.Post', cancelPermission: 'Pages.StockPilot.Sales.Cancel',
    toInput: (head, lines) => ({
      documentDate: head.date, issueType: head.type, customerName: head.customer.trim(), currencyId: head.currencyId, exchangeRate: 0,
      lines: lines.map((l) => ({ itemMasterId: l.itemId, unitMasterId: l.unitId, quantity: Number(l.qty), unitPrice: Number(l.price) }))
    }),
    numberOf: (doc) => doc.documentNo, dateOf: (doc) => doc.documentDate, partyLabel: 'Customer', partyOf: (doc) => doc.customerName,
    docLabel: 'Sale / issue', sign: '−', linePrice: (line) => line.unitPrice
  }
};

/**
 * The mockup's editor(): the xl slide-over to receive (or sell) stock. Header fields, item search, lines with unit,
 * quantity stepper, price and line total, optional attachments, total, Discard / Save draft / Save & post.
 * With "Hide cost and values" on there is no price input, line total or money total: each line keeps the item's
 * default price (standard cost or sale price, per unit) without showing it, and the footer counts items instead.
 * preItemId adds that item as the first line (Receive stock from the item drawer); preQuantity sets its quantity in
 * the base unit (Reorder on the quantity dashboard).
 */
export default function DocumentEditor({ kind, preItemId, preQuantity, onClose, onSaved, onImport }) {
  // Without multiple currencies the document is in the base currency and there is no currency choice.
  const multiCurrency = isFeatureEnabled(FEATURES.multiCurrency);
  const hideCost = isCostHidden();
  const K = KINDS[kind];
  const [head, setHead] = useState({ date: day(0), partyId: null, currencyId: null, customer: '', type: 1 });
  const [suppliers, setSuppliers] = useState([]);
  const [currencies, setCurrencies] = useState([]);
  const [lines, setLines] = useState([]);
  const linesRef = useRef(lines);
  linesRef.current = lines;
  const [files, setFiles] = useState([]);
  const searchBox = useRef(null);
  const draftButton = useRef(null);
  const postButton = useRef(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const supplierCall = K.party === 'supplier' ? callService('Supplier', 'GetForCombo') : Promise.resolve({ items: [] });
    Promise.all([supplierCall, callService('Currency', 'GetForCombo'), loadBaseCurrency()])
      .then(([supplierResult, currencyResult, base]) => {
        const supplierList = supplierResult.items || [];
        setSuppliers(supplierList);
        setCurrencies(currencyResult.items || []);
        const first = supplierList[0];
        setHead((current) => ({ ...current, partyId: first ? first.id : null, currencyId: (multiCurrency && first && first.currencyId) || (base && base.id) || (currencyResult.items[0] || {}).id || null }));
      })
      .catch(() => { /* abp.ajax showed it */ });
    if (preItemId) addItem(preItemId, preQuantity); else setTimeout(() => searchBox.current && searchBox.current.focus(), 0);
  }, []);

  // A line keeps the item's units ({ unitId, code, factor to the base unit }) to price and convert.
  async function addItem(id, quantity) {
    if (linesRef.current.some((l) => l.itemId === id)) {
      setLines((current) => current.map((l) => (l.itemId === id ? { ...l, qty: (Number(l.qty) || 0) + 1 } : l)));
      searchBox.current && searchBox.current.focus();
      return;
    }
    try {
      const item = await callService('ItemMaster', 'GetItemDetail', { id });
      // Without unit conversions a line is in the base unit only.
      const units = isFeatureEnabled(FEATURES.unitConversions) && (item.units || []).length ? item.units.map((u) => ({ unitId: u.unitId, code: u.code, factor: u.factor }))
        : [{ unitId: item.baseUnitId, code: item.baseUnitCode, factor: 1 }];
      setLines((current) => current.some((l) => l.itemId === id) ? current : [...current, {
        itemId: id, name: item.itemName, pictureUrl: item.pictureUrl, onHand: item.quantityOnHand, baseUnitCode: item.baseUnitCode,
        units, unitId: item.baseUnitId, qty: quantity > 0 ? quantity : 1, base: K.basePrice(item) || 0, price: K.basePrice(item) || 0
      }]);
    } catch (_) { /* abp.ajax showed it */ }
    searchBox.current && searchBox.current.focus();
  }

  const setLine = (index, change) => setLines((current) => current.map((l, i) => (i === index ? { ...l, ...change } : l)));
  const setUnit = (index, unitId) => {
    const line = lines[index], unit = line.units.find((u) => u.unitId === unitId) || { factor: 1 };
    setLine(index, { unitId, price: Math.round(line.base * unit.factor * 10000) / 10000 });
  };
  const step = (index, delta) => setLine(index, { qty: Math.max(1, (Number(lines[index].qty) || 0) + delta) });
  const total = lines.reduce((sum, l) => sum + (Number(l.qty) || 0) * (Number(l.price) || 0), 0);
  // Sales: more than on hand (in base units) is shown in red; the server blocks posting it.
  const factorOf = (l) => (l.units.find((u) => u.unitId === l.unitId) || { factor: 1 }).factor;
  const over = (l) => K.checkStock && (Number(l.qty) || 0) * factorOf(l) > l.onHand + 1e-9;
  const currencyCode = (currencies.find((c) => c.id === head.currencyId) || {}).code || '';

  async function pickFiles(event) {
    const picked = await readDocuments(event.target.files);
    event.target.value = '';
    setFiles((current) => [...current, ...picked]);
  }

  async function save(andPost) {
    if (!lines.length) { notifyError(L('Add at least one item')); return; }
    if (K.party === 'supplier' && !head.partyId) { notifyError(L('Choose a supplier')); return; }
    if (K.party === 'customer' && !head.customer.trim()) { notifyError(L('Enter a customer name')); return; }
    if (lines.some((l) => !(Number(l.qty) > 0))) { notifyError(L('Quantities must be above zero')); return; }
    const button = andPost ? postButton.current : draftButton.current;
    setSaving(true);
    setBusy(button, andPost ? L('Posting…') : L('Saving…'));
    let id = null, number = '';
    try {
      id = (await callService(K.service, 'Create', K.toInput(head, lines))).id;
      for (const file of files) await callService(K.service, 'UploadDocument', { purchaseInvoiceId: id, fileName: file.name, base64Content: file.base64 });
      const doc = await callService(K.service, 'Get', { id });
      number = K.numberOf(doc);
      if (andPost) {
        await callService(K.service, 'Post', { id });
        notifySuccess(L('{0} posted. Stock levels are updated.', K.numberOf(doc)));
      } else {
        notifySuccess(L('{0} saved as draft', K.numberOf(doc)));
      }
      onSaved(id);
      onClose();
    } catch (_) {
      // abp.ajax showed the server's message (e.g. not enough stock to post). If the document was created, it stays
      // as a draft, as in the mockup ("… saved as draft", then the posting error).
      if (id) { if (number) notifySuccess(L('{0} saved as draft', number)); onSaved(id); onClose(); }
    } finally {
      setSaving(false);
      if (button) clearBusy(button);
    }
  }

  const header = <div className="nm"><div className="av lg" style={{ background: K.colour }}>{K.icon}</div>
    <div><b style={{ fontSize: 17 }}>{L(K.title)}</b><div className="mu" style={{ fontWeight: 400 }}>{L(K.sub)}</div></div></div>;
  const actions = K.canImport && onImport ? <button className="btn s" onClick={onImport}><Icon name="up" size={15} />{L('Import from Excel')}</button> : null;
  const footer = <>
    {hideCost
      ? <b style={{ fontSize: 17 }}>{L(lines.length === 1 ? '{0} item · quantity {1}' : '{0} items · quantity {1}', lines.length, n(lines.reduce((sum, l) => sum + (Number(l.qty) || 0), 0)))}</b>
      : <b style={{ fontSize: 17 }}>{L('Total: {0}', `${currencyCode} ${n(total)}`)}</b>}
    <div className="ac">
      <button className="btn s" onClick={onClose}>{L('Discard')}</button>
      <button className="btn s" ref={draftButton} disabled={saving} onClick={() => save(false)}>{L('Save draft')}</button>
      {isGranted(K.postPermission) && <button className="btn g" ref={postButton} disabled={saving} onClick={() => save(true)}>{L('Save & post')}</button>}
    </div>
  </>;

  return <Modal size="xl" header={header} actions={actions} onClose={onClose} footer={footer} footerStyle={{ justifyContent: 'space-between', alignItems: 'center' }}>
    <div className="fm" style={{ gridTemplateColumns: 'repeat(auto-fit,minmax(170px,1fr))' }}>
      <label className="fl">{L('Date')}<input type="date" value={head.date} onChange={(event) => setHead({ ...head, date: event.target.value })} /></label>
      {K.party === 'supplier' ? <label className="fl">{L('Supplier')}<select value={head.partyId ?? ''} onChange={(event) => {
        const supplier = suppliers.find((s) => s.id === Number(event.target.value));
        setHead({ ...head, partyId: Number(event.target.value), currencyId: (multiCurrency && supplier && supplier.currencyId) || head.currencyId });
      }}>{suppliers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</select></label>
        : <>
          <label className="fl">{L('Customer')}<input placeholder={L('e.g. Walk-in customer')} value={head.customer} onChange={(event) => setHead({ ...head, customer: event.target.value })} /></label>
          <label className="fl">{L('Type')}<select value={head.type} onChange={(event) => setHead({ ...head, type: Number(event.target.value) })}>
            {ISSUE_TYPES.map(([value, label]) => <option key={value} value={value}>{L(label)}</option>)}</select></label>
        </>}
      {multiCurrency && <label className="fl">{L('Currency')}<select value={head.currencyId ?? ''} onChange={(event) => setHead({ ...head, currencyId: Number(event.target.value) })}>
        {currencies.map((c) => <option key={c.id} value={c.id}>{c.code}</option>)}</select></label>}
    </div>
    <ItemSearch ref={searchBox} onPick={(item) => addItem(item.id)} />
    <div>{lines.length ? lines.map((l, i) => <div className={hideCost ? 'ln nc' : 'ln'} key={l.itemId}>
      <div className="nm"><Avatar name={l.name} src={l.pictureUrl} />
        <div><b>{l.name}</b>{over(l)
          ? <span className="warn">{L('Only {0} available', `${n(l.onHand, 2)} ${l.baseUnitCode}`)}</span>
          : <span>{L('In stock: {0}', `${n(l.onHand, 2)} ${l.baseUnitCode}`)}</span>}</div></div>
      <select value={l.unitId} onChange={(event) => setUnit(i, Number(event.target.value))}>{l.units.map((u) => <option key={u.unitId} value={u.unitId}>{u.code}</option>)}</select>
      <div className="stp"><button type="button" onClick={() => step(i, -1)}>−</button>
        <input type="number" value={l.qty} onChange={(event) => setLine(i, { qty: event.target.value === '' ? '' : Math.max(0, Number(event.target.value)) })} />
        <button type="button" onClick={() => step(i, 1)}>+</button></div>
      {!hideCost && <input type="number" value={l.price} title={L(K.priceTitle)} onChange={(event) => setLine(i, { price: event.target.value === '' ? '' : Math.max(0, Number(event.target.value)) })} />}
      {!hideCost && <b style={{ textAlign: 'right' }}>{n((Number(l.qty) || 0) * (Number(l.price) || 0))}</b>}
      <button type="button" className="x" aria-label={L('Remove')} onClick={() => setLines(lines.filter((_, j) => j !== i))}>×</button>
    </div>) : <div className="empty"><b>{L('No items yet')}</b>{L('Search above and press Enter to add your first item.')}</div>}
    {lines.some(over) && <p className="warn">{L('Some quantities are more than you have in stock, so posting will be blocked.')}</p>}</div>
    {K.attachments && isFeatureEnabled(FEATURES.purchaseAttachments) && isGranted(K.attachPermission) && <div style={{ marginTop: 10 }}>
      <label className="btn s"><Icon name="up" size={15} />{L('Attach documents (optional)')}<input type="file" multiple hidden accept={DOCUMENT_TYPES} onChange={pickFiles} /></label>
      <div>{files.map((f, i) => <div className="li" key={`${f.name}-${i}`}>
        <div className="nm"><div className="av" style={{ background: 'var(--ac2)', color: 'var(--ac)' }}><Icon name="file" size={15} /></div><div><b>{f.name}</b><span>{fileSize(f.size)}</span></div></div>
        <button className="lnk" onClick={() => setFiles(files.filter((_, j) => j !== i))}>{L('Remove')}</button>
      </div>)}</div>
    </div>}
  </Modal>;
}
