import { useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import DocumentDrawer from '../inventory-shared/document-drawer.jsx';
import DocumentEditor from '../inventory-shared/document-editor.jsx';
import { date, n } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import ImportDialog from '../inventory-shared/import-dialog.jsx';
import { FEATURES, isFeatureEnabled } from '../inventory-shared/features.js';
import { DocumentBadge, documentStatusText } from '../inventory-shared/status-badge.jsx';

const PERM = { create: 'Pages.StockPilot.Purchases.Create' };
const PILLS = [[null, 'All', 'allCount'], [0, 'Draft', 'draftCount'], [1, 'Posted', 'postedCount'], [2, 'Cancelled', 'cancelledCount']];

/**
 * The mockup's docs('purchase'): status pills with counts, the invoice grid (PurchaseInvoice.GetAll, Excel export on the
 * server), the document drawer (viewDoc), the editor and the Excel import.
 * #/purchase-invoices?new=1[&item=<id>[&qty=<base quantity>]] opens the editor (New menu, item drawer, dashboard
 * Reorder); ?open=<id> opens an invoice.
 */
export default function PurchasesPage() {
  const [status, setStatus] = useState(null);
  const [counts, setCounts] = useState(null);
  const [reload, setReload] = useState(0);
  const [openId, setOpenId] = useState(null);
  const [editor, setEditor] = useState(null); // { itemId, quantity }
  const [importing, setImporting] = useState(false);
  const canCreate = isGranted(PERM.create);

  useHashQuery((query) => {
    if (query.has('open')) setOpenId(Number(query.get('open')));
    if (query.has('new') && canCreate) setEditor({ itemId: query.has('item') ? Number(query.get('item')) : null, quantity: query.has('qty') ? Number(query.get('qty')) : null });
  });
  const changed = () => setReload((x) => x + 1);
  const canImport = isFeatureEnabled(FEATURES.purchaseExcelImport);
  const openImport = () => { setEditor(null); setOpenId(null); setImporting(true); };

  return <>
    <div className="hd">
      <div><h1>{L('Purchases')}</h1><p className="sub">{L('Goods received from suppliers. Posting a document updates stock.')}<span className="tag">PurchaseInvoice</span></p></div>
      {canCreate && <div className="ac">
        {canImport && <button className="btn s" onClick={openImport}><Icon name="up" size={15} />{L('Import from Excel')}</button>}
        <button className="btn" onClick={() => setEditor({ itemId: null })}><Icon name="plus" size={15} />{L('Receive stock')}</button>
      </div>}
    </div>
    <div className="pills">{PILLS.map(([key, label, count]) =>
      <button key={label} className={status === key ? 'on' : ''} onClick={() => setStatus(key)}>{L(label)} ({counts ? counts[count] : '…'})</button>)}</div>
    <AbpGrid service="PurchaseInvoice" exportMethod="GetAllToExcel" name="Purchases" input={status === null ? {} : { status }} reloadKey={reload}
      onLoaded={setCounts} onRowClick={(row) => setOpenId(row.id)}
      filters={[
        { key: 'from', param: 'dateFrom', type: 'date', label: 'From' },
        { key: 'to', param: 'dateTo', type: 'date', label: 'To' },
        { key: 'supplier', param: 'supplierId', all: 'All suppliers', number: true, load: () => callService('Supplier', 'GetForCombo').then((r) => r.items.map((s) => [s.id, s.name])) }
      ]}
      columns={[
        { key: 'invoiceNo', label: 'Number', render: (r) => <b>{r.invoiceNo}</b> },
        { key: 'invoiceDate', label: 'Date', text: (r) => date(r.invoiceDate) },
        { key: 'supplierName', label: 'Supplier' },
        { key: 'lineCount', label: 'Items', right: true },
        { key: 'attachmentCount', label: 'Documents', right: true, render: (r) => (r.attachmentCount ? <span className="b OK"><Icon name="file" size={12} /> {r.attachmentCount}</span> : '') },
        { key: 'totalAmount', label: 'Total', right: true, cost: true, text: (r) => `${r.currencyCode} ${n(r.totalAmount)}` },
        { key: 'status', label: 'Status', text: (r) => documentStatusText(r.status), render: (r) => <DocumentBadge status={r.status} /> }
      ]} />
    {openId && <DocumentDrawer kind="purchase" id={openId} onClose={() => setOpenId(null)} onChanged={changed} />}
    {editor && <DocumentEditor kind="purchase" preItemId={editor.itemId} preQuantity={editor.quantity} onClose={() => setEditor(null)} onImport={canImport ? openImport : undefined}
      onSaved={() => { setStatus(null); changed(); }} />}
    {importing && <ImportDialog onClose={() => setImporting(false)} onImported={() => { setStatus(null); changed(); }} />}
  </>;
}
