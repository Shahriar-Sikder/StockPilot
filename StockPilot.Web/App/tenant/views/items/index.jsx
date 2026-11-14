import { useEffect, useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import Avatar from '../inventory-shared/avatar.jsx';
import { money, useBaseSymbol } from '../inventory-shared/currency.js';
import Drawer, { DrawerHeader } from '../inventory-shared/drawer.jsx';
import { agoParts, n } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { movementTypeText, StockBadge, stockStatusText } from '../inventory-shared/status-badge.jsx';
import { RecordDialog, useCombos } from '../generic-crud/index.jsx';
import { LISTS } from '../generic-crud/pages.js';
import IfCost from '../inventory-shared/if-cost.jsx';

const ITEM = LISTS.itemMaster;
const PILLS = [[0, 'All', 'allCount'], [1, 'In stock', 'inStockCount'], [2, 'Needs reorder', 'needsReorderCount']];
const go = (route) => { window.location.hash = `#${route}`; };

/**
 * The mockup's items() + openItem(): status pills with counts, the server-driven item grid (ItemMaster.GetAll,
 * which also returns the pill counts), and the item drawer (ItemMaster.GetItemDetail).
 * #/items?open=<id> opens an item's drawer (palette); #/items?new=1 opens Add item (New menu).
 */
export default function ItemsPage() {
  const { combos, load, refresh } = useCombos(ITEM);
  const [status, setStatus] = useState(0);
  const [counts, setCounts] = useState(null);
  const [reload, setReload] = useState(0);
  const [openId, setOpenId] = useState(null);
  const [dialog, setDialog] = useState(null); // { row } — row null to add
  const canCreate = isGranted(ITEM.canCreate);

  useHashQuery((query) => {
    if (query.has('open')) setOpenId(Number(query.get('open')));
    if (query.has('new') && canCreate) setDialog({ row: null });
  });

  const saved = () => { setReload((x) => x + 1); refresh(); };
  const comboFilter = (key, param, all, service) => ({ key, param, all, number: true, load: () => load(service).then((list) => list.map(([id, label]) => [id, label])) });

  return <>
    <div className="hd">
      <div><h1>{L('Items')}</h1><p className="sub">{L('Click an item to see its stock, history and quick actions.')}<span className="tag">ItemMaster · StockBalance</span></p></div>
      <div className="ac">{canCreate && <button className="btn" onClick={() => setDialog({ row: null })}><Icon name="plus" size={15} />{L('Add item')}</button>}</div>
    </div>
    <div className="pills">{PILLS.map(([key, label, count]) =>
      <button key={key} className={status === key ? 'on' : ''} onClick={() => setStatus(key)}>{L(label)} ({counts ? counts[count] : '…'})</button>)}</div>
    <AbpGrid service="ItemMaster" exportMethod="GetAllToExcel" pdf name="Items" input={{ status }} reloadKey={reload}
      onLoaded={setCounts} onRowClick={(row) => setOpenId(row.id)}
      filters={[
        comboFilter('category', 'itemCategoryId', 'All categories', 'ItemCategory'),
        comboFilter('sub', 'itemSubCategoryId', 'All sub-categories', 'ItemSubCategory')
      ]}
      columns={[
        { key: 'itemCode', label: 'Code' },
        { key: 'itemName', label: 'Item', render: (r) => <div className="nm"><Avatar name={r.itemName} src={r.pictureUrl} /><b>{r.itemName}</b></div> },
        { key: 'itemCategoryName', label: 'Category' },
        { key: 'itemSubCategoryName', label: 'Sub-category', text: (r) => r.itemSubCategoryName || '-' },
        { key: 'quantityOnHand', label: 'On hand', right: true, render: (r) => <b>{n(r.quantityOnHand, 2)}</b> },
        { key: 'baseUnitCode', label: 'Unit' },
        { key: 'levelRatio', label: 'Level', noexport: true, render: (r) => <div className={`bar ${r.status === 1 ? 'w' : r.status === 2 ? 'e' : ''}`}><i style={{ width: `${Math.min(100, Math.round(r.levelRatio * 100))}%` }} /></div> },
        { key: 'stockValue', label: 'Value', right: true, cost: true },
        { key: 'status', label: 'Status', text: (r) => stockStatusText(r.status), render: (r) => <StockBadge status={r.status} /> }
      ]} />
    {openId && <ItemDrawer id={openId} reloadKey={reload} onClose={() => setOpenId(null)}
      onEdit={(detail) => { setOpenId(null); setDialog({ row: { id: detail.id, pictureUrl: detail.pictureUrl } }); }} />}
    {dialog && <RecordDialog config={ITEM} row={dialog.row} combos={combos} onClose={() => setDialog(null)} onSaved={saved} />}
  </>;
}

/** The mockup's openItem() drawer. */
function ItemDrawer({ id, reloadKey, onClose, onEdit }) {
  const [detail, setDetail] = useState(null);
  const symbol = useBaseSymbol();
  useEffect(() => {
    let live = true;
    setDetail(null);
    callService('ItemMaster', 'GetItemDetail', { id }).then((result) => { if (live) setDetail(result); }).catch(onClose);
    return () => { live = false; };
  }, [id, reloadKey]);

  if (!detail) {
    return <Drawer onClose={onClose}>
      <DrawerHeader avatar={<div className="av lg sk" />} title={<span className="sk" style={{ display: 'inline-block', width: 180, height: 18 }} />} onClose={onClose} />
      <div className="db"><Skeleton /></div>
    </Drawer>;
  }

  const unit = detail.baseUnitCode;
  const units = (detail.units || []).map((u) => u.code).join(', ') || unit;
  const ago = (value) => { const [key, count] = agoParts(value); return L(key, count); };
  return <Drawer onClose={onClose}>
    <DrawerHeader avatar={<Avatar name={detail.itemName} size="lg" src={detail.pictureUrl} />}
      title={detail.itemName} subtitle={`${detail.itemCode} · ${detail.itemCategoryName || '-'} / ${detail.itemSubCategoryName || '-'}`} onClose={onClose} />
    <div className="db">
      <StockBadge status={detail.status} />
      <div className="g3" style={{ marginTop: 14 }}>
        <div className="card" style={{ margin: 0 }}><span className="mu">{L('On hand')}</span><h2>{n(detail.quantityOnHand, 2)} <small className="mu">{unit}</small></h2></div>
        <IfCost>
          <div className="card" style={{ margin: 0 }}><span className="mu">{L('Avg cost')}</span><h2>{money(symbol, n(detail.averageCost))}</h2></div>
          <div className="card" style={{ margin: 0 }}><span className="mu">{L('Value')}</span><h2>{money(symbol, n(detail.stockValue, 0))}</h2></div>
        </IfCost>
      </div>
      <div className="dl">
        <IfCost><div><span>{L('Sale price')}</span>{money(symbol, n(detail.salePrice))}</div></IfCost>
        <div><span>{L('Reorder level')}</span>{n(detail.reorderLevel)} {unit}</div>
        <div><span>{L('Base unit')}</span>{unit}</div>
        <div><span>{L('Buy / sell in')}</span>{units}</div>
      </div>
      <h3 style={{ marginTop: 22 }}>{L('Recent activity')}</h3>
      {(detail.recentMovements || []).map((m) => {
        const incoming = m.quantity > 0;
        return <div className="fd" key={m.id}>
          <i className={incoming ? 'in' : 'out'}>{incoming ? '+' : '−'}</i>
          <div style={{ flex: 1 }}><b>{movementTypeText(m.movementType)}</b><div className="mu">{m.referenceNo} · {ago(m.movementDate)}</div></div>
          <div style={{ textAlign: 'right' }}><b className={incoming ? 'pos' : 'neg'}>{incoming ? '+' : ''}{n(m.quantity, 2)}</b><div className="mu">{L('bal {0}', n(m.balanceAfter, 2))}</div></div>
        </div>;
      })}
    </div>
    <div className="df">
      {isGranted('Pages.StockPilot.Purchases.Create') && <button className="btn g" onClick={() => go(`/purchase-invoices?new=1&item=${detail.id}`)}><Icon name="purchase" size={16} />{L('Receive stock')}</button>}
      {isGranted('Pages.StockPilot.Sales.Create') && <button className="btn s" onClick={() => go(`/issues?new=1&item=${detail.id}`)}><Icon name="sale" size={16} />{L('Issue / sell')}</button>}
      {isGranted(ITEM.canEdit) && <button className="btn s" onClick={() => onEdit(detail)}>{L('Edit item')}</button>}
    </div>
  </Drawer>;
}
