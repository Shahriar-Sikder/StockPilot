import { useEffect, useState } from 'react';
import { callService, L } from '../abp.js';
import { isCostHidden } from '../inventory-shared/cost-visibility.js';
import { loadBaseCurrency, money } from '../inventory-shared/currency.js';
import { n } from '../inventory-shared/format.js';
import Kpi from '../inventory-shared/kpi.jsx';
import { AttentionList, DashboardFailed, DashboardHeader, DashboardSkeleton, HealthDonut, InOutChart, InOutLegend, RecentActivity } from './parts.jsx';
import QuantityDashboard from './quantity-dashboard.jsx';

/** The quantity dashboard while "Hide cost and values" is on, else the value dashboard. */
export default function InventoryDashboardPage(props) {
  return isCostHidden() ? <QuantityDashboard {...props} /> : <ValueDashboard {...props} />;
}

/**
 * The mockup's dash(): greeting and quick actions, four KPIs, stock in vs out for the last 14 days, stock health,
 * what needs attention (drafts to post, items to reorder) and recent stock activity — all from Dashboard.GetSummary.
 */
function ValueDashboard({ user }) {
  const [summary, setSummary] = useState(null);
  const [base, setBase] = useState(null);

  useEffect(() => {
    let live = true;
    Promise.all([callService('Dashboard', 'GetSummary'), loadBaseCurrency()])
      .then(([result, currency]) => { if (live) { setSummary(result); setBase(currency); } })
      .catch(() => { if (live) setSummary(false); });
    return () => { live = false; };
  }, []);

  const symbol = base ? base.symbol || base.code : '';
  const amount = (value, currencyCode) => (!currencyCode || (base && currencyCode === base.code) ? money(symbol, n(value, 0)) : `${currencyCode} ${n(value, 0)}`);
  const header = <DashboardHeader user={user} />;

  if (summary === null) return <>{header}<DashboardSkeleton /></>;
  if (summary === false) return <>{header}<DashboardFailed /></>;

  return <>
    {header}
    <div className="kp">
      <Kpi icon="coin" colour="var(--ac)" background="var(--ac2)" label={L('Stock value')} value={summary.stockValue} prefix={symbol ? `${symbol} ` : ''} small={L('{0} items tracked', summary.trackedItemCount)} />
      <Kpi icon="purchase" colour="var(--ok)" background="var(--okb)" label={L('Stock in (14 days)')} value={summary.stockInTotal} prefix={symbol ? `${symbol} ` : ''} small={L('Purchases received')} valueClass="pos" />
      <Kpi icon="sale" colour="var(--wn)" background="var(--wnb)" label={L('Stock out (14 days)')} value={summary.stockOutTotal} prefix={symbol ? `${symbol} ` : ''} small={L('Sales and issues at cost')} valueStyle={{ color: 'var(--wn)' }} />
      <Kpi icon="alert" colour="var(--er)" background="var(--erb)" label={L('Needs attention')} value={summary.lowStockCount + summary.draftCount} countUp={false}
        small={L('{0} low · {1} waiting to post', summary.lowStockCount, summary.draftCount)} />
    </div>
    <div className="g2">
      <div className="card"><h3>{L('Stock in vs out · last 14 days')}</h3><InOutChart days={summary.days} inOf={(d) => d.stockIn} outOf={(d) => d.stockOut} /><InOutLegend /></div>
      <div className="card"><h3>{L('Stock health')}</h3><HealthDonut health={summary.health} /></div>
    </div>
    <div className="g2">
      <AttentionList attention={summary.attention} draftDetail={(a) => amount(a.totalAmount, a.currencyCode)} />
      <RecentActivity movements={summary.recentMovements} />
    </div>
  </>;
}
