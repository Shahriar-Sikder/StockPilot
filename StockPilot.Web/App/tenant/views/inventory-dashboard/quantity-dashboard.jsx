import { useEffect, useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import Avatar from '../inventory-shared/avatar.jsx';
import { n } from '../inventory-shared/format.js';
import Icon from '../inventory-shared/icons.jsx';
import Kpi from '../inventory-shared/kpi.jsx';
import { AttentionList, DashboardFailed, DashboardHeader, DashboardSkeleton, go, HealthDonut, InOutChart, InOutLegend, RecentActivity } from './parts.jsx';
import './quantity-dashboard.css';

// InsightSeverity (StockPilot.Core\Inventory): badge class and text. InsightActionType: 1 Reorder, 2 ReviewDraft.
const SEVERITY = { 1: ['crit', 'Critical'], 2: ['warn', 'Warning'], 3: ['info', 'Info'] };
const REORDER = 1, REVIEW = 2;

/** Message parameters come as invariant strings; numbers are shown with the app's number format. */
const param = (value) => (/^-?\d+(\.\d+)?$/.test(value) ? n(Number(value), 2) : value);

/**
 * The quantity dashboard ("Hide cost and values"): the value dashboard's layout and styles with quantities, counts
 * and days instead of money — Dashboard.GetQuantitySummary. Item quantities are in each item's base unit.
 */
export default function QuantityDashboard({ user }) {
  const [summary, setSummary] = useState(null);

  useEffect(() => {
    let live = true;
    callService('Dashboard', 'GetQuantitySummary')
      .then((result) => { if (live) setSummary(result); })
      .catch(() => { if (live) setSummary(false); });
    return () => { live = false; };
  }, []);

  const header = <DashboardHeader user={user} />;
  if (summary === null) return <>{header}<DashboardSkeleton /></>;
  if (summary === false) return <>{header}<DashboardFailed /></>;

  return <>
    {header}
    <div className="kp">
      <Kpi icon="items" colour="var(--ac)" background="var(--ac2)" label={L('Total items')} value={summary.totalItems} small={L('Active stock items')} />
      <ItemKpi icon="purchase" colour="var(--ok)" background="var(--okb)" label={L('Most in')} item={summary.mostIn} sign="+" empty={L('Nothing came in')} />
      <ItemKpi icon="sale" colour="var(--wn)" background="var(--wnb)" label={L('Most out')} item={summary.mostOut} sign="−" empty={L('Nothing went out')} />
      <Kpi icon="alert" colour="var(--er)" background="var(--erb)" label={L('Needs attention')} value={summary.lowStockCount + summary.draftCount} countUp={false}
        small={L('{0} low · {1} waiting to post', summary.lowStockCount, summary.draftCount)} />
    </div>
    <div className="g2">
      <div className="card"><h3>{L('Stock in vs out · last 14 days')} {L('(movements)')}</h3>
        <InOutChart days={summary.inOutChart} inOf={(d) => d.inCount} outOf={(d) => d.outCount} /><InOutLegend /></div>
      <div className="card"><h3>{L('Stock health')}</h3><HealthDonut health={summary.stockHealth} /></div>
    </div>
    <div className="g2">
      <TopItems title={L('Top 5 received this month')} items={summary.topReceivedThisMonth} countText={(c) => L(c === 1 ? '{0} invoice' : '{0} invoices', c)} empty={L('Nothing received yet this month.')} />
      <TopItems title={L('Top 5 sales this month')} items={summary.topSoldThisMonth} countText={(c) => L(c === 1 ? '{0} sale' : '{0} sales', c)} empty={L('No sales yet this month.')} />
    </div>
    <Suggestions insights={summary.insights} />
    <div className="g2">
      <AttentionList attention={summary.attention} draftDetail={(a) => a.partyName} />
      <RecentActivity movements={summary.recentActivity} />
    </div>
  </>;
}

/** A KPI card whose figure is an item: avatar and name, then "+qty unit · last 14 days". */
function ItemKpi({ icon, colour, background, label, item, sign, empty }) {
  return <div>
    <i className="ki" style={{ color: colour, background }}><Icon name={icon} size={16} /></i>
    <span>{label}</span>
    {item
      ? <><b className="qd-item"><Avatar name={item.itemName} src={item.pictureUrl} /><em title={item.itemName}>{item.itemName}</em></b>
        <small>{`${sign}${n(item.quantity, 2)} ${item.baseUnitCode}`} · {L('last 14 days')}</small></>
      : <><b className="qd-item">—</b><small>{empty} · {L('last 14 days')}</small></>}
  </div>;
}

/** "Top 5 …" card: avatar, name, quantity and documents, and a bar against the first (largest) quantity. */
function TopItems({ title, items, countText, empty }) {
  const top = items.length ? items[0].quantity : 1;
  return <div className="card"><h3>{title}</h3>
    {items.length ? items.map((item) => <div className="li" key={item.itemId}>
      <div className="nm"><Avatar name={item.itemName} src={item.pictureUrl} />
        <div><b>{item.itemName}</b><span>{n(item.quantity, 2)} {item.baseUnitCode} · {countText(item.documentCount)}</span></div></div>
      <div className="bar"><i style={{ width: `${Math.max(4, Math.round(item.quantity / (top || 1) * 100))}%` }} /></div>
    </div>) : <div className="empty qd-empty"><b>{L('Nothing yet')}</b>{empty}</div>}
  </div>;
}

/** "Suggestions": severity badge, item, localized message and the action (Reorder with the suggested quantity, or Review). */
function Suggestions({ insights }) {
  const canReorder = isGranted('Pages.StockPilot.Purchases.Create');
  return <div className="card"><h3>{L('Suggestions')}</h3>
    {insights.length ? insights.map((insight, i) => {
      const [badge, severity] = SEVERITY[insight.severity] || SEVERITY[3];
      const message = L(insight.messageKey, ...(insight.messageParams || []).map(param));
      return <div className="li" key={`${insight.type}-${insight.itemId || insight.documentId || i}`}>
        <div className="qd-sg">
          <span className={`b qd-${badge}`}>{L(severity)}</span>
          <div>{insight.itemName && <b>{insight.itemName}</b>}<div className="mu">{message}</div></div>
        </div>
        {insight.actionType === REORDER && canReorder && insight.itemId &&
          <button className="btn s" onClick={() => go(`/purchase-invoices?new=1&item=${insight.itemId}${insight.suggestedQuantity ? `&qty=${insight.suggestedQuantity}` : ''}`)}>
            {insight.suggestedQuantity ? L('Reorder {0}', n(insight.suggestedQuantity, 2)) : L('Reorder')}</button>}
        {insight.actionType === REVIEW && insight.documentId &&
          <button className="btn s" onClick={() => go(`${insight.documentType === 'PurchaseInvoice' ? '/purchase-invoices' : '/issues'}?open=${insight.documentId}`)}>{L('Review')}</button>}
      </div>;
    }) : <p className="mu">{L('No suggestions right now. Stock in and out look balanced.')}</p>}
  </div>;
}
