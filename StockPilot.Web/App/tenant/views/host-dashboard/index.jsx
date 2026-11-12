import { useEffect, useState } from 'react';
import { callService, isGranted, L } from '../abp.js';
import { date, n } from '../inventory-shared/format.js';
import Icon from '../inventory-shared/icons.jsx';
import Kpi from '../inventory-shared/kpi.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { daysLeftText, go, UsageBar } from '../host-shared/index.jsx';

/**
 * The host's landing page (HostDashboard.GetSummary): store counts, waiting requests, subscriptions ending soon and
 * stores at 80% or more of a limit.
 */
export default function HostDashboardPage({ user }) {
  const [summary, setSummary] = useState(null);
  useEffect(() => {
    callService('HostDashboard', 'GetSummary', {}).then(setSummary).catch(() => setSummary(false));
  }, []);
  const firstName = String((user && (user.name || user.userName)) || '').split(' ')[0];
  const canRequests = isGranted('Pages.Tenants.Requests');

  const header = <div className="hd">
    <div><h1>{L('Good day, {0}', firstName)}</h1><p className="sub">{L('Your stores, their requests and subscriptions at a glance.')}</p></div>
    <div className="ac">
      {canRequests && <button className="btn l" onClick={() => go('/host/requests')}><Icon name="file" size={17} />{L('Store requests')}</button>}
      {isGranted('Pages.Tenants') && <button className="btn s l" onClick={() => go('/host/tenants')}><Icon name="supplier" size={17} />{L('Stores')}</button>}
    </div>
  </div>;

  if (summary === null) return <>{header}<Skeleton /></>;
  if (summary === false) return <>{header}<div className="card"><div className="empty"><b>{L('Something went wrong')}</b>{L('Reload the page to try again.')}</div></div></>;

  const openTenant = (id) => go(`/host/tenants?open=${id}`);
  return <>
    {header}
    <div className="kp">
      <Kpi icon="supplier" colour="var(--ok)" background="var(--okb)" label={L('Active stores')} value={summary.activeTenants} small={L('Can sign in')} />
      <Kpi icon="lock" colour="var(--mu)" background="var(--bg)" label={L('Inactive stores')} value={summary.inactiveTenants} small={L('Switched off')} />
      <Kpi icon="file" colour="var(--ac)" background="var(--ac2)" label={L('Pending requests')} value={summary.pendingRequests} small={L('Waiting for your review')} />
      <Kpi icon="alert" colour="var(--wn)" background="var(--wnb)" label={L('Ending soon')} value={summary.expiringSoon} small={L('Subscriptions ending in {0} days', summary.warnBeforeDays)} />
    </div>
    <div className="g2">
      <div className="card">
        <h3>{L('Latest pending requests')}</h3>
        {summary.latestPendingRequests.length ? summary.latestPendingRequests.map((r) => <div className="li" key={r.id}>
          <div className="nm"><div><b>{r.storeName}</b><span>{r.requestNo} · {r.tenancyName}{r.city ? ` · ${r.city}` : ''} · {date(r.creationTime)}</span></div></div>
          {canRequests && <button className="btn s" onClick={() => go(`/host/requests?open=${r.id}`)}>{L('Review')}</button>}
        </div>) : <div className="hs-empty-line">{L('No requests are waiting.')}</div>}
      </div>
      <div className="card">
        <h3>{L('Subscriptions ending soon')}</h3>
        {summary.tenantsExpiringSoon.length ? summary.tenantsExpiringSoon.map((t) => <div className="li row" key={t.tenantId} onClick={() => openTenant(t.tenantId)} style={{ cursor: 'pointer' }}>
          <div className="nm"><div><b>{t.name}</b><span>{t.tenancyName}</span></div></div>
          <div style={{ textAlign: 'right' }}><b className="hs-soon">{date(t.subscriptionEndDate)}</b><div className="mu">{daysLeftText(t.subscriptionEndDate)}</div></div>
        </div>) : <div className="hs-empty-line">{L('No subscription ends in the next {0} days.', summary.warnBeforeDays)}</div>}
      </div>
    </div>
    <div className="card">
      <h3>{L('Stores near a limit')}</h3>
      {summary.tenantsNearLimit.length ? summary.tenantsNearLimit.map((t) => <div className="li row" key={t.tenantId} onClick={() => openTenant(t.tenantId)} style={{ cursor: 'pointer', alignItems: 'flex-start', flexWrap: 'wrap' }}>
        <div className="nm" style={{ minWidth: 160 }}><div><b>{t.name}</b><span>{t.tenancyName}</span></div></div>
        <div style={{ flex: '1 1 260px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(180px,1fr))', gap: '0 18px' }}>
          {t.maxUserCount > 0 && <UsageBar label={L('Users')} used={t.userCount} limit={t.maxUserCount} />}
          {t.maxItemCount > 0 && <UsageBar label={L('Items')} used={t.itemCount} limit={t.maxItemCount} />}
        </div>
      </div>) : <div className="hs-empty-line">{L('No store is at {0}% of a limit.', n(80, 0))}</div>}
    </div>
  </>;
}
