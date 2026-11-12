import { useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import { notify, notifyAction } from '../toast.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import Avatar from '../inventory-shared/avatar.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import Drawer, { DrawerHeader } from '../inventory-shared/drawer.jsx';
import Icon from '../inventory-shared/icons.jsx';
import Kpi from '../inventory-shared/kpi.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { DocumentBadge, ImportBadge, StockBadge, stockStatusText } from '../inventory-shared/status-badge.jsx';
import { date, n } from '../inventory-shared/format.js';
import template from './index.html?raw';

/**
 * #/ui-kit — not in the menu. Shows the shared components (frontend Phase 2) working against real app services,
 * for side-by-side checks with docs/mockups/stockpilot-v2.html until the pages that use them exist.
 */
export default function UiKitPage() {
  const [drawer, setDrawer] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [skeleton, setSkeleton] = useState(false);
  const [reload, setReload] = useState(0);
  const [pill, setPill] = useState(0);
  const [counts, setCounts] = useState({ allCount: 0, inStockCount: 0, needsReorderCount: 0 });

  async function deleteCountry(row) {
    await callService('Country', 'Delete', { id: row.id });
    setReload((x) => x + 1);
    notifyAction('info', L('Deleted'), L('Undo'), async () => { await callService('Country', 'Restore', { id: row.id }); setReload((x) => x + 1); });
  }

  const actions = <>
    <button className="btn s" onClick={() => setDrawer(true)}><Icon name="items" size={15} />Drawer</button>
    <button className="btn s" onClick={() => setConfirmOpen(true)}>Confirm</button>
    <button className="btn s" onClick={() => notifyAction('info', 'Deleted', 'Undo', () => notify('success', 'Restored'))}>Toast + Undo</button>
    <button className="btn s" onClick={() => setSkeleton(!skeleton)}>Skeleton</button>
  </>;

  return <>
    <HtmlView template={template} values={{ title: escapeHtml('UI kit'), sub: escapeHtml('Shared components on live data (not in the menu).') }}>{actions}</HtmlView>
    <div className="kp">
      <Kpi icon="coin" colour="var(--ac)" background="var(--ac2)" label="Stock value" value={233416} prefix="৳ " small="6 items tracked" />
      <Kpi icon="purchase" colour="var(--ok)" background="var(--okb)" label="Stock in (14 days)" value={124260} prefix="৳ " small="Purchases received" valueClass="pos" />
      <Kpi icon="sale" colour="var(--wn)" background="var(--wnb)" label="Stock out (14 days)" value={421094} prefix="৳ " small="Sales and issues at cost" valueStyle={{ color: 'var(--wn)' }} />
      <Kpi icon="alert" colour="var(--er)" background="var(--erb)" label="Needs attention" value={4} countUp={false} small="2 low · 2 waiting to post" />
    </div>
    {skeleton ? <Skeleton /> : <div className="card">
      <h3>Badges and avatars</h3>
      <div className="ac">
        <DocumentBadge status={0} /><DocumentBadge status={1} /><DocumentBadge status={2} />
        <StockBadge status={0} /><StockBadge status={1} /><StockBadge status={2} /><StockBadge status={3} />
        <ImportBadge status={0} /><ImportBadge status={1} /><ImportBadge status={2} />
        <Avatar name="Smartphone X1" /><Avatar name="Green Tea" /><Avatar name="Black Coffee" size="lg" />
        <div className="bar"><i style={{ width: '70%' }} /></div><div className="bar w"><i style={{ width: '30%' }} /></div><div className="bar e"><i style={{ width: '4%' }} /></div>
      </div>
    </div>}

    {isGranted('Pages.StockPilot.Items') && <>
      <div className="pills">{[[0, 'All', counts.allCount], [1, 'In stock', counts.inStockCount], [2, 'Needs reorder', counts.needsReorderCount]].map(([key, label, count]) =>
        <button key={key} className={pill === key ? 'on' : ''} onClick={() => setPill(key)}>{L(label)} ({count})</button>)}</div>
      <AbpGrid service="ItemMaster" exportMethod="GetAllToExcel" pdf name="Items" input={{ status: pill }} defaultSorting="itemName asc"
        onLoaded={(result) => setCounts(result)}
        filters={[{ key: 'category', param: 'itemCategoryId', all: 'All categories', number: true, load: () => callService('ItemCategory', 'GetForCombo').then((r) => r.items.map((c) => [c.id, c.name])) }]}
        onRowClick={() => setDrawer(true)}
        columns={[
          { key: 'itemCode', label: 'Code' },
          { key: 'itemName', label: 'Item', render: (r) => <div className="nm"><Avatar name={r.itemName} picture={{ service: 'ItemMaster', id: r.id, pictureId: r.pictureId }} /><b>{r.itemName}</b></div> },
          { key: 'itemCategoryName', label: 'Category' },
          { key: 'quantityOnHand', label: 'On hand', right: true, render: (r) => <b>{n(r.quantityOnHand, 2)}</b> },
          { key: 'baseUnitCode', label: 'Unit' },
          { key: 'levelRatio', label: 'Level', noexport: true, render: (r) => <div className={`bar ${r.status === 1 ? 'w' : r.status === 2 ? 'e' : ''}`}><i style={{ width: `${Math.min(100, Math.round(r.levelRatio * 100))}%` }} /></div> },
          { key: 'stockValue', label: 'Value', right: true, render: (r) => n(r.stockValue) },
          { key: 'status', label: 'Status', text: (r) => stockStatusText(r.status), render: (r) => <StockBadge status={r.status} /> }
        ]} />
    </>}

    <AbpGrid service="Country" name="Countries" pdf reloadKey={reload} pageSize={10} input={{ includeDeleted: true }}
      filters={[{ key: 'active', param: 'isActive', all: 'All', options: [['true', 'Active'], ['false', 'Inactive']] }]}
      rowClass={(r) => (r.isDeleted ? 'del' : '')}
      columns={[
        { key: 'code', label: 'Code' },
        { key: 'name', label: 'Name' },
        { key: 'phoneCode', label: 'Phone code' },
        { key: 'creationTime', label: 'Created', text: (r) => date(r.creationTime) },
        { key: '_a', label: '', nosort: true, noexport: true, stop: true, render: (r) => r.isDeleted
          ? <button className="lnk b2" onClick={async () => { await callService('Country', 'Restore', { id: r.id }); setReload((x) => x + 1); notify('success', L('Restored')); }}>{L('Restore')}</button>
          : <><button className="lnk b2"><Icon name="edit" size={13} />{L('Edit')}</button><button className="lnk" onClick={() => deleteCountry(r)}><Icon name="trash" size={13} />{L('Delete')}</button></> }
      ]} />
    <p className="mu">{L('Deleting only marks a record as deleted, so Undo always works.')}</p>

    {drawer && <Drawer onClose={() => setDrawer(false)}>
      <DrawerHeader avatar={<Avatar name="Wireless Earbuds" size="lg" />} title="Wireless Earbuds" subtitle="ITM-002 · Electronics / Accessories" onClose={() => setDrawer(false)} />
      <div className="db"><StockBadge status={1} /><div className="g3" style={{ marginTop: 14 }}>
        <div className="card" style={{ margin: 0 }}><span className="mu">{L('On hand')}</span><h2>7 <small className="mu">PCS</small></h2></div>
        <div className="card" style={{ margin: 0 }}><span className="mu">{L('Avg cost')}</span><h2>৳ 1,800</h2></div>
        <div className="card" style={{ margin: 0 }}><span className="mu">{L('Value')}</span><h2>৳ 12,600</h2></div>
      </div></div>
      <div className="df"><button className="btn g"><Icon name="purchase" size={16} />{L('Receive stock')}</button><button className="btn s"><Icon name="sale" size={16} />{L('Issue / sell')}</button><button className="btn s">{L('Edit item')}</button></div>
    </Drawer>}

    {confirmOpen && <ConfirmDialog title="Post PI-0101?" okText={L('Post to stock')} okClass="g" busyText={L('Posting…')}
      onConfirm={() => new Promise((resolve) => setTimeout(resolve, 900)).then(() => notify('success', 'PI-0101 posted. Stock levels are updated.'))}
      onClose={() => setConfirmOpen(false)}>
      <p style={{ marginTop: 0 }}>This updates stock immediately:</p><p><b>+30 PCS Wireless Earbuds</b></p><p className="mu">You can cancel it later; that writes reversing entries.</p>
    </ConfirmDialog>}
  </>;
}
