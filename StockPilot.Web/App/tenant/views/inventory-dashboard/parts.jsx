import { isGranted, L } from '../abp.js';
import Avatar from '../inventory-shared/avatar.jsx';
import { agoParts, date, n } from '../inventory-shared/format.js';
import Icon from '../inventory-shared/icons.jsx';
import { movementTypeText } from '../inventory-shared/status-badge.jsx';

// Pieces both dashboards share: the value dashboard (index.jsx) and the quantity dashboard (quantity-dashboard.jsx).

export const go = (route) => { window.location.hash = `#${route}`; };
export const ago = (value) => { const [key, count] = agoParts(value); return L(key, count); };

/** Greeting and the Receive stock / Sell or issue quick actions. */
export function DashboardHeader({ user }) {
  const firstName = String((user && (user.name || user.userName)) || '').split(' ')[0];
  return <div className="hd">
    <div><h1>{L('Good day, {0}', firstName)}</h1><p className="sub">{L('Here is how your stock looks today.')}</p></div>
    <div className="ac">
      {isGranted('Pages.StockPilot.Purchases.Create') && <button className="btn l" onClick={() => go('/purchase-invoices?new=1')}><Icon name="purchase" size={17} />{L('Receive stock')}</button>}
      {isGranted('Pages.StockPilot.Sales.Create') && <button className="btn s l" onClick={() => go('/issues?new=1')}><Icon name="sale" size={17} />{L('Sell or issue')}</button>}
    </div>
  </div>;
}

export function DashboardFailed() {
  return <div className="card"><div className="empty"><b>{L('Something went wrong')}</b>{L('Reload the page to try again.')}</div></div>;
}

/**
 * The mockup's 14-day bar chart: in and out side by side per day, scaled to the largest value, day of month below.
 * inOf / outOf read a day's two values (values at cost, or movement counts).
 */
export function InOutChart({ days, inOf, outOf }) {
  const max = Math.max(1, ...days.map(inOf), ...days.map(outOf));
  return <svg viewBox="0 0 600 196" width="100%" style={{ display: 'block' }}>
    {days.map((d, i) => {
      const x = 14 + i * 42, hIn = inOf(d) / max * 150, hOut = outOf(d) / max * 150;
      return <g key={i}>
        <rect x={x} y={170 - hIn} width="15" height={hIn} rx="3" fill="var(--in)"><title>{`${date(d.date)} · ${L('Stock in')}: ${n(inOf(d), 0)}`}</title></rect>
        <rect x={x + 17} y={170 - hOut} width="15" height={hOut} rx="3" fill="var(--out)"><title>{`${date(d.date)} · ${L('Stock out')}: ${n(outOf(d), 0)}`}</title></rect>
        <text x={x + 16} y="190" textAnchor="middle" fontSize="10" fill="var(--mu)">{date(d.date).slice(8)}</text>
      </g>;
    })}
  </svg>;
}

export function InOutLegend() {
  return <div className="lgd"><span><i style={{ background: 'var(--in)' }} />{L('Stock in')}</span><span><i style={{ background: 'var(--out)' }} />{L('Stock out')}</span></div>;
}

/** The mockup's donut: In stock / Low stock / Out of stock shares of the tracked items, with the count in the middle. */
export function HealthDonut({ health }) {
  const total = health.okCount + health.lowCount + health.outCount;
  const C = 2 * Math.PI * 42;
  let offset = 0;
  const parts = [['okCount', 'In stock', 'var(--ok)'], ['lowCount', 'Low stock', 'var(--out)'], ['outCount', 'Out of stock', 'var(--er)']];
  const segments = parts.map(([key, , colour]) => {
    const length = health[key] / (total || 1) * C;
    const circle = <circle key={key} cx="60" cy="60" r="42" fill="none" stroke={colour} strokeWidth="16" strokeDasharray={`${length} ${C - length}`} strokeDashoffset={-offset} transform="rotate(-90 60 60)" />;
    offset += length;
    return circle;
  });
  return <div style={{ display: 'flex', gap: 20, alignItems: 'center', flexWrap: 'wrap' }}>
    <svg viewBox="0 0 120 120" width="130" height="130">{segments}
      <text x="60" y="58" textAnchor="middle" fontSize="22" fontWeight="700" fill="var(--tx)">{total}</text>
      <text x="60" y="74" textAnchor="middle" fontSize="10" fill="var(--mu)">{L('items')}</text></svg>
    <div>{parts.map(([key, label, colour]) => <div key={key} style={{ margin: '6px 0' }}>
      <i style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 3, background: colour, marginRight: 8 }} />{L(label)}: <b>{health[key]}</b></div>)}</div>
  </div>;
}

/**
 * "Needs your attention": drafts to post (Review) and low / out items (Reorder).
 * draftDetail(row) is the middle of a draft's caption (the value dashboard shows the total there).
 */
export function AttentionList({ attention, draftDetail }) {
  const drafts = attention.filter((a) => a.kind !== 3);
  const lows = attention.filter((a) => a.kind === 3);
  return <div className="card"><h3>{L('Needs your attention')}</h3>
    {drafts.map((a) => <div className="li" key={`d${a.kind}-${a.id}`}>
      <div><b>{a.documentNo}</b> <span className="b Draft">{L('Draft')}</span>
        <div className="mu">{[L(a.kind === 1 ? 'Purchase' : 'Sale'), draftDetail(a), L('not posted yet')].filter(Boolean).join(' · ')}</div></div>
      <button className="btn s" onClick={() => go(`${a.kind === 1 ? '/purchase-invoices' : '/issues'}?open=${a.id}`)}>{L('Review')}</button>
    </div>)}
    {lows.map((a) => <div className="li" key={`l${a.id}`}>
      <div className="nm"><Avatar name={a.itemName} src={a.pictureUrl} />
        <div><b>{a.itemName}</b><span>{L('{0} left · reorder at {1}', `${n(a.quantityOnHand, 2)} ${a.baseUnitCode}`, n(a.reorderLevel))}</span></div></div>
      {isGranted('Pages.StockPilot.Purchases.Create') && <button className="btn s" onClick={() => go(`/purchase-invoices?new=1&item=${a.id}`)}>{L('Reorder')}</button>}
    </div>)}
    {!drafts.length && !lows.length && <p className="mu">{L('You are all caught up.')}</p>}
  </div>;
}

/** "Recent activity": the last movements, quantities only. */
export function RecentActivity({ movements }) {
  return <div className="card"><h3>{L('Recent activity')}</h3>
    {movements.map((m) => {
      const incoming = m.quantity > 0;
      return <div className="fd" key={m.id}>
        <i className={incoming ? 'in' : 'out'}>{incoming ? '+' : '−'}</i>
        <div style={{ flex: 1, minWidth: 0 }}><b>{movementTypeText(m.movementType)} · {m.itemName}</b>
          <div className="mu">{n(Math.abs(m.quantity), 2)} {m.baseUnitCode} · {m.referenceNo} · {ago(m.movementDate)}</div></div>
      </div>;
    })}
  </div>;
}

/** The mockup's skel(): heading bar, four KPI placeholders and a card of lines. */
export function DashboardSkeleton() {
  return <>
    <div className="kp">{[0, 1, 2, 3].map((i) => <div key={i}><div className="sk" style={{ height: 12, width: 90, marginBottom: 10 }} /><div className="sk" style={{ height: 22, width: 120 }} /></div>)}</div>
    <div className="card">{[0, 1, 2, 3, 4, 5, 6].map((i) => <div key={i} className="sk" style={{ height: 14, margin: '12px 0' }} />)}</div>
  </>;
}
