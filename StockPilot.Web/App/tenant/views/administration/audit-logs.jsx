import { useState } from 'react';
import { L } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import Drawer, { DrawerHeader } from '../inventory-shared/drawer.jsx';
import { day, n } from '../inventory-shared/format.js';

/**
 * Audit logs as a mockup grid: AuditLog.GetAuditLogs pages, sorts and filters on the server, and
 * GetAuditLogsToExcel exports the same filters. The search box filters by user name. A row opens the
 * details (parameters, browser, exception) in the drawer.
 *
 * StartDate / EndDate are required by GetAuditLogsInput, so the last 30 days are sent until a date is chosen.
 */
const startOf = (value) => `${value}T00:00:00`;
const endOf = (value) => `${value}T23:59:59.999`;
const when = (value) => (value ? String(value).replace('T', ' ').slice(0, 19) : '');

const RESULTS = [['false', 'Success'], ['true', 'Error']];

function ResultBadge({ row }) {
  return row.exception ? <span className="b Out">{L('Error')}</span> : <span className="b OK">{L('Success')}</span>;
}

export default function AuditLogsPage() {
  const [open, setOpen] = useState(null);
  return <>
    <div className="hd"><div><h1>{L('Audit logs')}</h1><p className="sub">{L('Every call to the server: who made it, when, how long it took and whether it failed.')}<span className="tag">AuditLog</span></p></div></div>
    <AbpGrid service="AuditLog" method="GetAuditLogs" exportMethod="GetAuditLogsToExcel" name="Audit logs"
      searchParam="userName" defaultSorting="executionTime desc"
      input={{ startDate: startOf(day(-30)), endDate: endOf(day(0)) }}
      filters={[
        { key: 'from', param: 'startDate', type: 'date', label: 'From', map: startOf },
        { key: 'to', param: 'endDate', type: 'date', label: 'To', map: endOf },
        { key: 'result', param: 'hasException', all: 'All results', options: RESULTS.map(([value, label]) => [value, L(label)]), map: (value) => value === 'true' }
      ]}
      onRowClick={setOpen}
      columns={[
        { key: 'executionTime', label: 'Date and time', text: (r) => when(r.executionTime) },
        { key: 'userName', label: 'User', text: (r) => r.userName || '' },
        { key: 'serviceName', label: 'Service' },
        { key: 'methodName', label: 'Action' },
        { key: 'executionDuration', label: 'Duration (ms)', right: true },
        { key: 'clientIpAddress', label: 'IP address' },
        { key: 'exception', label: 'Result', nosort: true, text: (r) => L(r.exception ? 'Error' : 'Success'), render: (r) => <ResultBadge row={r} /> }
      ]} />
    {open && <AuditDrawer row={open} onClose={() => setOpen(null)} />}
  </>;
}

function AuditDrawer({ row, onClose }) {
  return <Drawer onClose={onClose}>
    <DrawerHeader title={`${row.serviceName || ''}.${row.methodName || ''}`} subtitle={when(row.executionTime)} badge={<ResultBadge row={row} />} onClose={onClose} />
    <div className="db">
      <div className="dl">
        <div><span>{L('User')}</span>{row.userName || '—'}</div>
        <div><span>{L('Duration (ms)')}</span>{n(row.executionDuration, 0)}</div>
        <div><span>{L('IP address')}</span>{row.clientIpAddress || '—'}</div>
        <div><span>{L('Client')}</span>{row.clientName || '—'}</div>
        {row.impersonatorUserId && <div><span>{L('Impersonated by user')}</span>{row.impersonatorUserId}</div>}
      </div>
      <h3 className="al-h">{L('Browser')}</h3>
      <p className="al-text">{row.browserInfo || '—'}</p>
      <h3 className="al-h">{L('Parameters')}</h3>
      <pre className="al-pre">{pretty(row.parameters) || '—'}</pre>
      {row.customData && <><h3 className="al-h">{L('Custom data')}</h3><pre className="al-pre">{row.customData}</pre></>}
      {row.exception && <><h3 className="al-h">{L('Error')}</h3><pre className="al-pre er">{row.exception}</pre></>}
    </div>
    <div className="df"><span /><button className="btn s" onClick={onClose}>{L('Close')}</button></div>
  </Drawer>;
}

function pretty(json) {
  if (!json) return '';
  try { return JSON.stringify(JSON.parse(json), null, 2); } catch (_) { return json; }
}
