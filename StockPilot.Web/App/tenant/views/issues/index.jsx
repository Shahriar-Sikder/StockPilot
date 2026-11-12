import { useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import DocumentDrawer from '../inventory-shared/document-drawer.jsx';
import DocumentEditor from '../inventory-shared/document-editor.jsx';
import { date, n } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import { DocumentBadge, documentStatusText } from '../inventory-shared/status-badge.jsx';

const PILLS = [[null, 'All', 'allCount'], [0, 'Draft', 'draftCount'], [1, 'Posted', 'postedCount'], [2, 'Cancelled', 'cancelledCount']];

/**
 * The mockup's docs('sale'): status pills with counts, the sales and issues grid (IssueSale.GetAll, Excel export on the
 * server), the document drawer and the sell / issue editor (stock warnings; the server blocks posting more than on hand).
 * #/issues?new=1[&item=<id>] opens the editor (New menu, item drawer); ?open=<id> opens a document.
 */
export default function IssuesPage() {
  const [status, setStatus] = useState(null);
  const [counts, setCounts] = useState(null);
  const [reload, setReload] = useState(0);
  const [openId, setOpenId] = useState(null);
  const [editor, setEditor] = useState(null); // { itemId }
  const canCreate = isGranted('Pages.StockPilot.Sales.Create');

  useHashQuery((query) => {
    if (query.has('open')) setOpenId(Number(query.get('open')));
    if (query.has('new') && canCreate) setEditor({ itemId: query.has('item') ? Number(query.get('item')) : null });
  });
  const changed = () => setReload((x) => x + 1);

  return <>
    <div className="hd">
      <div><h1>{L('Sales & issues')}</h1><p className="sub">{L('Goods sold or issued out of stock. Posting a document updates stock.')}<span className="tag">IssueSale</span></p></div>
      {canCreate && <div className="ac"><button className="btn" onClick={() => setEditor({ itemId: null })}><Icon name="plus" size={15} />{L('New sale / issue')}</button></div>}
    </div>
    <div className="pills">{PILLS.map(([key, label, count]) =>
      <button key={label} className={status === key ? 'on' : ''} onClick={() => setStatus(key)}>{L(label)} ({counts ? counts[count] : '…'})</button>)}</div>
    <AbpGrid service="IssueSale" exportMethod="GetAllToExcel" name="Sales and issues" input={status === null ? {} : { status }} reloadKey={reload}
      onLoaded={setCounts} onRowClick={(row) => setOpenId(row.id)}
      filters={[
        { key: 'from', param: 'dateFrom', type: 'date', label: 'From' },
        { key: 'to', param: 'dateTo', type: 'date', label: 'To' },
        { key: 'customer', param: 'customerName', all: 'All customers', load: () => callService('IssueSale', 'GetCustomers', { maxResultCount: 500 }).then((r) => r.items.map((name) => [name, name])) }
      ]}
      columns={[
        { key: 'documentNo', label: 'Number', render: (r) => <b>{r.documentNo}</b> },
        { key: 'documentDate', label: 'Date', text: (r) => date(r.documentDate) },
        { key: 'customerName', label: 'Customer' },
        { key: 'lineCount', label: 'Items', right: true },
        { key: 'totalAmount', label: 'Total', right: true, cost: true, text: (r) => `${r.currencyCode} ${n(r.totalAmount)}` },
        { key: 'status', label: 'Status', text: (r) => documentStatusText(r.status), render: (r) => <DocumentBadge status={r.status} /> }
      ]} />
    {openId && <DocumentDrawer kind="sale" id={openId} onClose={() => setOpenId(null)} onChanged={changed} />}
    {editor && <DocumentEditor kind="sale" preItemId={editor.itemId} onClose={() => setEditor(null)} onSaved={() => { setStatus(null); changed(); }} />}
  </>;
}
