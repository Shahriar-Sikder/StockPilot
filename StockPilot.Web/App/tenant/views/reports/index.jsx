import { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { callService, L, notifyError } from '../abp.js';
import { saveBlob } from '../api.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { withoutCost } from '../inventory-shared/cost-visibility.js';
import { FEATURES, isFeatureEnabled } from '../inventory-shared/features.js';
import { loadBaseCurrency, money } from '../inventory-shared/currency.js';
import { ISSUE_TYPES } from '../inventory-shared/document-editor.jsx';
import { date, day, n } from '../inventory-shared/format.js';
import Icon from '../inventory-shared/icons.jsx';
import IfCost from '../inventory-shared/if-cost.jsx';
import { documentStatusText, movementTypeText, stockStatusText } from '../inventory-shared/status-badge.jsx';
import { getWorkspaceInfo } from '../inventory-shared/workspace.js';

const SERVICE = 'InventoryReport';
const STATUS_ALL = [['', 'All'], [0, 'Draft'], [1, 'Posted'], [2, 'Cancelled']];
const STOCK_ALL = [['', 'All'], [0, 'In stock'], [1, 'Low stock'], [2, 'Out of stock']];
const num = (key, label, decimals, cost = false) => ({ key, label, right: true, cost, text: (r) => r[key], render: (r) => (r[key] === null || r[key] === undefined ? '' : n(r[key], decimals)) });
/** One box of the totals strip; cost: a cost, value or margin (left out while costs are hidden). */
const total = (label, value, cost = false) => ({ label, value, cost });
const optionalNumber = (value) => (value ? value : null); // the ledger shows an empty cell instead of 0 in In / Out

/**
 * The mockup's REP table: one entry per report with its InventoryReport method, parameters (p), grid columns and
 * the totals strip (tot). Every report also has a server Excel export (Get…ToExcel with the same input).
 */
const REPORTS = {
  stock: {
    name: 'Stock item list', desc: 'Current stock, cost and value of every item.', plainDesc: 'Current stock of every item.', method: 'GetStockItemList', p: ['cat', 'stat'],
    input: (P) => ({ itemCategoryId: P.cat === '' ? null : P.cat, status: P.status === '' ? null : P.status }),
    rows: (result) => result.rows,
    cols: [{ key: 'itemCode', label: 'Code' }, { key: 'itemName', label: 'Item' }, { key: 'itemCategoryName', label: 'Category' }, { key: 'baseUnitCode', label: 'Unit' },
      num('quantityOnHand', 'On hand'), num('averageCost', 'Avg cost', 2, true), num('stockValue', 'Value', 2, true), num('reorderLevel', 'Reorder level'),
      { key: 'status', label: 'Status', text: (r) => stockStatusText(r.status) }],
    tot: (result, symbol) => [total('Items', result.itemCount), total('Total value', money(symbol, n(result.totalValue)), true)]
  },
  ledger: {
    name: 'Item ledger', desc: 'Every movement of one item with a running balance.', method: 'GetItemLedger', p: ['dates', 'item'],
    input: (P) => ({ itemMasterId: P.item, dateFrom: P.from, dateTo: P.to }),
    // The mockup starts the ledger with an "Opening balance" row on the From date.
    rows: (result, P) => [{ id: 'open', movementDate: P.from, referenceNo: '', description: L('Opening balance'), quantityIn: null, quantityOut: null, unitCost: null, runningBalance: result.openingQuantity }]
      .concat(result.rows.map((r) => ({ ...r, id: r.movementId, description: movementTypeText(r.movementType), quantityIn: optionalNumber(r.quantityIn), quantityOut: optionalNumber(r.quantityOut) }))),
    cols: [{ key: 'movementDate', label: 'Date', text: (r) => date(r.movementDate) }, { key: 'referenceNo', label: 'Reference' }, { key: 'description', label: 'Description' },
      num('quantityIn', 'In'), num('quantityOut', 'Out'), num('unitCost', 'Unit cost', 2, true), num('runningBalance', 'Balance')],
    tot: (result) => [total('Item', `${result.itemName} (${result.baseUnitCode})`), total('Opening', n(result.openingQuantity)), total('Total in', n(result.totalIn)), total('Total out', n(result.totalOut)), total('Closing', n(result.closingQuantity))]
  },
  invoices: {
    name: 'Invoice list', desc: 'Purchase invoices in a date range.', method: 'GetInvoiceList', p: ['dates', 'supplier', 'status'],
    input: (P) => ({ dateFrom: P.from, dateTo: P.to, supplierId: P.supplier === '' ? null : P.supplier, status: P.status === '' ? null : P.status }),
    rows: (result) => result.rows.map((r) => ({ ...r, id: r.invoiceId })),
    cols: [{ key: 'invoiceNo', label: 'Invoice no' }, { key: 'invoiceDate', label: 'Date', text: (r) => date(r.invoiceDate) }, { key: 'supplierName', label: 'Supplier' },
      num('lineCount', 'Items'), { key: 'currencyCode', label: 'Currency' }, num('totalAmount', 'Total', 2, true), { key: 'status', label: 'Status', text: (r) => documentStatusText(r.status) }],
    tot: (result) => [total('Invoices', result.invoiceCount), total('Total (excluding cancelled)', n(result.postedTotal + result.draftTotal), true)]
  },
  sales: {
    name: 'Sales list', desc: 'Sales and issues with cost and margin.', plainDesc: 'Sales and issues in a date range.', method: 'GetSalesList', p: ['dates', 'status', 'type'],
    input: (P) => ({ dateFrom: P.from, dateTo: P.to, status: P.status === '' ? null : P.status, issueType: P.type === '' ? null : P.type }),
    rows: (result) => result.rows.map((r) => ({ ...r, id: r.documentId })),
    cols: [{ key: 'documentNo', label: 'Number' }, { key: 'documentDate', label: 'Date', text: (r) => date(r.documentDate) }, { key: 'customerName', label: 'Customer' },
      { key: 'issueType', label: 'Type', text: (r) => L((ISSUE_TYPES.find(([value]) => value === r.issueType) || [0, ''])[1]) }, num('lineCount', 'Items'),
      num('totalAmount', 'Total', 2, true), num('cost', 'Cost', 2, true), num('margin', 'Margin', 2, true), { key: 'status', label: 'Status', text: (r) => documentStatusText(r.status) }],
    tot: (result) => [total('Documents', result.documentCount), total('Sales total', n(result.rows.filter((r) => r.status !== 2).reduce((s, r) => s + r.totalAmount, 0)), true),
      total('Cost (posted)', n(result.postedCostOfSales), true), total('Margin (posted)', n(result.postedMargin), true)]
  },
  i2s: {
    name: 'Invoice to sales', desc: 'How much of each purchase invoice has been sold (first in, first out).', method: 'GetInvoiceToSales', p: ['dates', 'itemAll'],
    input: (P) => ({ dateFrom: P.from, dateTo: P.to, itemMasterId: P.item === '' ? null : P.item }),
    // "Sold" is everything that left from this receipt (sales, issues and other outs), as in the mockup's FIFO.
    rows: (result) => result.rows.map((r, i) => ({ ...r, id: `${r.invoiceId}-${r.itemId}-${i}`, used: r.soldQuantity + r.issuedQuantity + r.otherOutQuantity })),
    cols: [{ key: 'invoiceNo', label: 'Invoice' }, { key: 'invoiceDate', label: 'Date', text: (r) => date(r.invoiceDate) }, { key: 'itemName', label: 'Item' }, { key: 'baseUnitCode', label: 'Unit' },
      num('receivedQuantity', 'Received'), num('used', 'Sold'), num('remainingQuantity', 'Remaining'), num('unitCost', 'Unit cost', 2, true),
      num('costOfSold', 'Cost of sold', 2, true), num('salesValue', 'Sales value*', 2, true), num('margin', 'Margin*', 2, true)],
    tot: (result) => [total('Invoices lines', result.rows.length), total('Cost of sold', n(result.totalCostOfSold), true), total('Sales value*', n(result.totalSalesValue), true), total('Margin*', n(result.totalMargin), true), total('* at current sale price', '', true)]
  }
};

const initialParams = () => ({ from: day(-30), to: day(0), item: '', supplier: '', status: '', cat: '', type: '' });
const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const SHEET_CSS = 'body{font:12px Arial,sans-serif;margin:24px;color:#000}.shh{display:flex;justify-content:space-between;border-bottom:2px solid #000;padding-bottom:8px;margin-bottom:10px}h1{font-size:18px;margin:0}table{width:100%;border-collapse:collapse}th{background:#eee;border-bottom:1px solid #000;padding:4px 6px;text-align:left}td{padding:4px 6px;border-bottom:1px solid #ddd}.r{text-align:right}';

/**
 * The mockup's reports(): report picker (.rpc), parameters with Save printable copy / Print, the totals strip (.sm)
 * and the result grid. Results come from InventoryReport (whole lists, so the grid searches and pages them in the
 * browser); Export Excel runs the report's Get…ToExcel on the server; Print renders the sheet into #prn.
 */
export default function ReportsPage() {
  const [key, setKey] = useState('stock');
  const [params, setParams] = useState(initialParams);
  const [result, setResult] = useState(null);
  const [lists, setLists] = useState({ items: [], categories: [], suppliers: [] });
  const [symbol, setSymbol] = useState('');
  const view = useRef([]);
  const R = REPORTS[key];

  useEffect(() => {
    Promise.all([callService('ItemMaster', 'GetForCombo'), callService('ItemCategory', 'GetForCombo'), callService('Supplier', 'GetForCombo'), loadBaseCurrency()])
      .then(([items, categories, suppliers, base]) => {
        setLists({ items: items.items.map((i) => [i.id, i.itemName]), categories: categories.items.map((c) => [c.id, c.name]), suppliers: suppliers.items.map((s) => [s.id, s.name]) });
        if (base) setSymbol(base.symbol || base.code);
      })
      .catch(() => { /* shown by abp.ajax */ });
  }, []);

  // The ledger needs one item: the first one, as in the mockup's rpPick().
  const ready = !(key === 'ledger' && params.item === '');
  useEffect(() => { if (key === 'ledger' && params.item === '' && lists.items.length) setParams((p) => ({ ...p, item: lists.items[0][0] })); }, [key, lists.items]);

  const input = useMemo(() => R.input(params), [key, params]);
  useEffect(() => {
    if (!ready) return undefined;
    let live = true;
    setResult(null);
    callService(SERVICE, R.method, input).then((r) => { if (live) setResult(r); }).catch(() => { if (live) setResult({ failed: true }); });
    return () => { live = false; };
  }, [key, JSON.stringify(input), ready]);

  const pick = (next) => { if (next === key) return; setResult(null); setKey(next); setParams((p) => ({ ...p, item: '', status: '', type: '', supplier: '', cat: '' })); };
  const set = (name, value) => setParams((p) => ({ ...p, [name]: ['item', 'supplier', 'cat', 'status', 'type'].includes(name) && value !== '' ? Number(value) : value }));
  const ok = result && !result.failed;
  const rows = useMemo(() => (ok ? R.rows(result, params) : null), [result, key]);
  const totals = ok ? withoutCost(R.tot(result, symbol)) : [];

  // The mockup's ptxt(): what the printed copy was filtered by.
  function paramText() {
    const label = (list, id) => (list.find(([value]) => value === id) || [0, ''])[1];
    const parts = [];
    if (R.p.includes('dates')) parts.push(L('Period: {0} to {1}', params.from, params.to));
    if ((R.p.includes('item') || R.p.includes('itemAll')) && params.item !== '') parts.push(`${L('Item')}: ${label(lists.items, params.item)}`);
    if (R.p.includes('supplier') && params.supplier !== '') parts.push(`${L('Supplier')}: ${label(lists.suppliers, params.supplier)}`);
    if (R.p.includes('cat') && params.cat !== '') parts.push(`${L('Category')}: ${label(lists.categories, params.cat)}`);
    if (R.p.includes('stat') && params.status !== '') parts.push(`${L('Status')}: ${stockStatusText(params.status)}`);
    if (R.p.includes('status') && params.status !== '') parts.push(`${L('Status')}: ${documentStatusText(params.status)}`);
    if (R.p.includes('type') && params.type !== '') parts.push(`${L('Type')}: ${L(ISSUE_TYPES.find(([v]) => v === params.type)[1])}`);
    return parts.join(' · ') || L('All records');
  }

  // The mockup's sheet(): the printable copy of the rows as currently searched and sorted.
  function sheet() {
    const cols = withoutCost(R.cols);
    const cell = (c, r) => { const v = c.text ? c.text(r) : r[c.key]; return typeof v === 'number' ? n(v) : (v ?? ''); };
    const now = new Date();
    const printed = `${day(0)} ${now.toTimeString().slice(0, 5)}`;
    return `<div class="sh"><div class="shh"><div><h1>${esc(L(R.name))}</h1><b>${esc(getWorkspaceInfo().tenantName)}</b></div><div style="text-align:right">${esc(L('Printed'))} ${printed}<br>${esc(paramText())}</div></div>`
      + `<table><thead><tr>${cols.map((c) => `<th class="${c.right ? 'r' : ''}">${esc(L(c.label))}</th>`).join('')}</tr></thead>`
      + `<tbody>${view.current.map((r) => `<tr>${cols.map((c) => `<td class="${c.right ? 'r' : ''}">${esc(cell(c, r))}</td>`).join('')}</tr>`).join('')}</tbody></table>`
      + `<p><b>${totals.map(({ label, value }) => esc(L(label)) + (value === '' ? '' : `: ${esc(value)}`)).join(' · ')}</b></p></div>`;
  }

  const [printing, setPrinting] = useState('');
  function print() {
    setPrinting(sheet());
    setTimeout(() => { try { window.print(); } catch (_) { notifyError(L('Printing is blocked in this view. Use Save printable copy.')); } }, 60);
  }
  function savePrintable() {
    const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>${esc(L(R.name))}</title><style>${SHEET_CSS}</style></head><body>${sheet()}</body></html>`;
    saveBlob(`${L(R.name).replace(/\W+/g, '_')}.html`, new Blob([html], { type: 'text/html' }));
  }

  const field = (label, control) => <label className="fl" key={label}>{L(label)}{control}</label>;
  const select = (name, options) => <select value={params[name]} onChange={(event) => set(name, event.target.value)}>
    {options.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>;
  const controls = [];
  if (R.p.includes('dates')) controls.push(field('From', <input type="date" value={params.from} onChange={(e) => set('from', e.target.value)} />), field('To', <input type="date" value={params.to} onChange={(e) => set('to', e.target.value)} />));
  if (R.p.includes('item')) controls.push(field('Item', select('item', lists.items)));
  if (R.p.includes('itemAll')) controls.push(field('Item', select('item', [['', L('All items')], ...lists.items])));
  if (R.p.includes('cat')) controls.push(field('Category', select('cat', [['', L('All categories')], ...lists.categories])));
  if (R.p.includes('supplier')) controls.push(field('Supplier', select('supplier', [['', L('All suppliers')], ...lists.suppliers])));
  if (R.p.includes('stat')) controls.push(field('Status', select('status', STOCK_ALL.map(([v, l]) => [v, L(l)]))));
  if (R.p.includes('status')) controls.push(field('Status', select('status', STATUS_ALL.map(([v, l]) => [v, L(l)]))));
  if (R.p.includes('type')) controls.push(field('Type', select('type', [['', L('All')], ...ISSUE_TYPES.map(([v, l]) => [v, L(l)])])));

  return <>
    <div className="hd"><div><h1>{L('Reports')}</h1><p className="sub">{L('Choose a report, set the filters, then print it or export it to Excel.')}</p></div></div>
    <div className="rpc">{Object.entries(REPORTS).map(([k, report]) =>
      <button key={k} className={key === k ? 'on' : ''} onClick={() => pick(k)}><b>{L(report.name)}</b><span><IfCost when={!!report.plainDesc} fallback={L(report.plainDesc)}>{L(report.desc)}</IfCost></span></button>)}</div>
    <div className="card"><div className="fg" style={{ display: 'flex', gap: 14, flexWrap: 'wrap', alignItems: 'flex-end' }}>
      {controls}<span className="sp" />
      {isFeatureEnabled(FEATURES.export) && <>
        <button className="btn s" disabled={!ok} onClick={savePrintable}><Icon name="dl" size={15} />{L('Save printable copy')}</button>
        <button className="btn" disabled={!ok} onClick={print}><Icon name="print" size={15} />{L('Print')}</button>
      </>}
    </div></div>
    <div className="sm">{ok ? totals.map(({ label, value }) => <div key={label}><span>{L(label)}</span><b>{value}</b></div>)
      : <div><span className="sk" style={{ display: 'inline-block', width: 160, height: 14 }} /></div>}</div>
    <AbpGrid key={key} service={SERVICE} exportMethod={`${R.method}ToExcel`} name={R.name} input={input} rows={ok ? rows : (result && result.failed ? [] : null)}
      rowKey="id" columns={R.cols} onView={(list) => { view.current = list; }} />
    {key === 'i2s' && <IfCost><p className="mu">{L('* Sales value and margin use each item’s current sale price, because stock history stores cost only.')}</p></IfCost>}
    {printing && <PrintArea html={printing} />}
  </>;
}

/** #prn must be a direct child of body: the print CSS hides every other body child. */
function PrintArea({ html }) {
  const [host] = useState(() => {
    let node = document.getElementById('prn');
    if (!node) { node = document.createElement('div'); node.id = 'prn'; document.body.appendChild(node); }
    return node;
  });
  return createPortal(<div dangerouslySetInnerHTML={{ __html: html }} />, host);
}
