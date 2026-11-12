import { useEffect, useState } from 'react';
import { callService, isAnyGranted, isGranted, L, notifySuccess } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import Drawer, { DrawerHeader } from '../inventory-shared/drawer.jsx';
import { date, day } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Modal from '../inventory-shared/modal.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { dateInput, differences, editionRows, FeatureTree, go, RequestBadge, rowValues, useEditions } from '../host-shared/index.jsx';

const PILLS = [['', 'All', 'total'], ['0', 'Pending', 'pending'], ['1', 'Approved', 'approved'], ['2', 'Rejected', 'rejected'], ['3', 'Cancelled', 'cancelled']];

const COLUMNS = [
  { key: 'requestNo', label: 'Request', render: (r) => <b>{r.requestNo}</b> },
  { key: 'storeName', label: 'Store', render: (r) => <div className="nm"><div><b>{r.storeName}</b><span>{r.tenancyName}</span></div></div> },
  { key: 'contactName', label: 'Contact', render: (r) => <div className="nm"><div><b>{r.contactName || '—'}</b><span>{r.contactPhone}</span></div></div> },
  { key: 'city', label: 'City' },
  { key: 'adminEmailAddress', label: 'Administrator' },
  { key: 'creationTime', label: 'Received', text: (r) => date(r.creationTime) },
  { key: 'status', label: 'Status', render: (r) => <RequestBadge status={r.status} /> }
];

const FILTERS = [
  { key: 'dateFrom', type: 'date', label: 'From' },
  { key: 'dateTo', type: 'date', label: 'To', map: (value) => `${value}T23:59:59` }
];

/**
 * Store requests (TenantRequest): status pills with counts, the grid, and a drawer per request with Edit details,
 * Approve (edition, active, subscription end, note, feature overrides) and Reject (reason required).
 */
export default function HostTenantRequestsPage() {
  const [status, setStatus] = useState('0');
  const [counts, setCounts] = useState(null);
  const [reload, setReload] = useState(0);
  const [openId, setOpenId] = useState(null);

  useEffect(() => { callService('TenantRequest', 'GetCounts', {}).then(setCounts).catch(() => { /* shown by abp.ajax */ }); }, [reload]);
  useHashQuery((query) => { if (query.get('open')) setOpenId(Number(query.get('open'))); });
  // The sidebar's "waiting for review" badge listens for this.
  const changed = () => { setReload((k) => k + 1); window.dispatchEvent(new Event('stockpilot:requests-changed')); };

  return <>
    <div className="hd"><div><h1>{L('Store requests')}</h1><p className="sub">{L('People who asked for a store. Review each one, then approve or reject it.')}</p></div></div>
    <div className="pills">{PILLS.map(([value, label, key]) => <button type="button" key={key} className={status === value ? 'on' : ''} onClick={() => setStatus(value)}>
      {L(label)}{counts ? ` · ${counts[key] ?? 0}` : ''}
    </button>)}</div>
    <AbpGrid service="TenantRequest" method="GetAll" columns={COLUMNS} filters={FILTERS} input={status === '' ? {} : { status: Number(status) }}
      defaultSorting="CreationTime desc" reloadKey={reload} onRowClick={(row) => setOpenId(row.id)} />
    {openId && <RequestDrawer id={openId} onClose={() => setOpenId(null)} onChanged={changed} />}
  </>;
}

function RequestDrawer({ id, onClose, onChanged }) {
  const [request, setRequest] = useState(null);
  const [dialog, setDialog] = useState('');
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let live = true;
    setRequest(null);
    callService('TenantRequest', 'GetForEdit', { id }).then((r) => { if (live) setRequest(r); }).catch(() => onClose());
    return () => { live = false; };
  }, [id, reload]);

  const after = (message) => { notifySuccess(message); setDialog(''); setReload((k) => k + 1); onChanged(); };

  if (!request) return <Drawer onClose={onClose}><DrawerHeader title={L('Store request')} onClose={onClose} /><div className="db"><Skeleton kpis={false} /></div></Drawer>;

  const pending = request.status === 0;
  const field = (label, value) => <div><span>{L(label)}</span>{value || '—'}</div>;
  return <Drawer onClose={onClose}>
    <DrawerHeader title={request.storeName} subtitle={`${request.requestNo} · ${L('Received {0}', date(request.creationTime))}`} badge={<RequestBadge status={request.status} />} onClose={onClose} />
    <div className="db">
      <div className="hs-section">{L('Store details')}</div>
      <div className="dl">
        {field('Store name', request.storeName)}{field('Workspace name', request.tenancyName)}
        {field('Contact person', request.contactName)}{field('Contact phone', request.contactPhone)}
        {field('Contact email', request.contactEmail)}{field('Business type', request.businessType)}
        {field('Address', request.address)}{field('City', [request.city, request.countryName].filter(Boolean).join(', '))}
      </div>
      {request.notes && <><div className="hs-section">{L('Notes')}</div><p style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{request.notes}</p></>}
      <div className="hs-section">{L('Administrator account')}</div>
      <div className="dl">
        {field('Name', [request.adminName, request.adminSurname].filter(Boolean).join(' '))}{field('Email', request.adminEmailAddress)}
        {field('Mobile number', request.adminPhoneNumber)}{field('Sent from', request.requesterIpAddress)}
      </div>
      {!pending && <><div className="hs-section">{L('Review')}</div>
        <div className="dl">{field('Reviewed', date(request.reviewedTime))}{field(request.status === 2 ? 'Reason' : 'Note', request.reviewNote)}</div></>}
      {request.status === 1 && request.createdTenantId && <button className="btn s" onClick={() => { onClose(); go(`/host/tenants?open=${request.createdTenantId}`); }}>{L('Open the store')}</button>}
    </div>
    <div className="df">
      <div className="hs-actions">
        {pending && <button className="btn s" onClick={() => setDialog('edit')}>{L('Edit details')}</button>}
        {!pending && request.status !== 1 && <button className="btn s d" onClick={() => setDialog('delete')}>{L('Delete')}</button>}
      </div>
      {pending && <div className="hs-actions">
        {isGranted('Pages.Tenants.Requests.Reject') && <button className="btn s d" onClick={() => setDialog('reject')}>{L('Reject')}</button>}
        {isGranted('Pages.Tenants.Requests.Approve') && <button className="btn" onClick={() => setDialog('approve')}>{L('Approve')}</button>}
      </div>}
    </div>
    {dialog === 'edit' && <EditRequestModal request={request} onClose={() => setDialog('')} onSaved={() => after(L('Request saved.'))} />}
    {dialog === 'approve' && <ApproveModal request={request} onClose={() => setDialog('')} onApproved={() => after(L('Store {0} created. The administrator was emailed.', request.tenancyName))} />}
    {dialog === 'reject' && <RejectModal request={request} onClose={() => setDialog('')} onRejected={() => after(L('Request rejected. The applicant was emailed.'))} />}
    {dialog === 'delete' && <ConfirmDialog title={L('Delete this request?')} okText={L('Delete')} okClass="d" busyText={L('Deleting…')} onClose={() => setDialog('')}
      onConfirm={async () => { await callService('TenantRequest', 'Delete', { id }); notifySuccess(L('Request deleted.')); onChanged(); onClose(); }}>
      <p style={{ margin: 0 }}>{L('{0} will be removed from the list. This cannot be undone.', request.requestNo)}</p>
    </ConfirmDialog>}
  </Drawer>;
}

/** A form of text fields; `fields` are [key, label, {type, required, full}]. */
function Fields({ fields, model, set }) {
  return <div className="fm hs-fm">{fields.map(([key, label, options = {}]) => <label key={key} className={options.full ? 'hs-full' : undefined}>
    <span>{L(label)}{options.required && <span className="req"> *</span>}</span>
    {options.type === 'textarea'
      ? <textarea value={model[key] ?? ''} onChange={(event) => set(key, event.target.value)} />
      : <input type={options.type || 'text'} required={options.required} value={model[key] ?? ''} onChange={(event) => set(key, event.target.value)} />}
  </label>)}</div>;
}

const REQUEST_FIELDS = [
  ['storeName', 'Store name', { required: true }], ['tenancyName', 'Workspace name', { required: true }],
  ['contactName', 'Contact person'], ['contactPhone', 'Contact phone'], ['contactEmail', 'Contact email', { type: 'email' }], ['businessType', 'Business type'],
  ['address', 'Address', { full: true }], ['city', 'City'], ['countryName', 'Country'],
  ['adminName', 'First name', { required: true }], ['adminSurname', 'Last name', { required: true }],
  ['adminEmailAddress', 'Administrator email', { type: 'email', required: true }], ['adminPhoneNumber', 'Mobile number'],
  ['notes', 'Notes', { type: 'textarea', full: true }]
];

function EditRequestModal({ request, onClose, onSaved }) {
  const [model, setModel] = useState(request);
  const [busy, setBusy] = useState(false);
  const set = (key, value) => setModel((m) => ({ ...m, [key]: value }));
  async function save(event) {
    event.preventDefault();
    setBusy(true);
    try { await callService('TenantRequest', 'Update', model); onSaved(); } catch (_) { setBusy(false); }
  }
  return <Modal title={L('Edit request {0}', request.requestNo)} size="wd" onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="request-edit" className="btn" disabled={busy}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save')}</button>
  </>}>
    <form id="request-edit" onSubmit={save}><Fields fields={REQUEST_FIELDS} model={model} set={set} /></form>
  </Modal>;
}

function ApproveModal({ request, onClose, onApproved }) {
  const editions = useEditions();
  const [model, setModel] = useState({ editionId: request.requestedEditionId ? String(request.requestedEditionId) : '', isActive: true, subscriptionEndDate: day(365), reviewNote: '' });
  const [rows, setRows] = useState(null);
  const [values, setValues] = useState({});
  const [busy, setBusy] = useState(false);
  const canSeeFeatures = isAnyGranted('Pages.Editions.Create', 'Pages.Editions.Edit');
  const set = (key, value) => setModel((m) => ({ ...m, [key]: value }));

  // Choose the first edition once they load, unless the request asked for one.
  useEffect(() => { if (!model.editionId && editions.length) set('editionId', editions[0][0]); }, [editions.length]);

  // The chosen edition's values are the defaults; what the host changes becomes the store's overrides.
  useEffect(() => {
    if (!canSeeFeatures) return;
    let live = true;
    setRows(null);
    callService('Edition', 'GetEditionForEdit', { id: model.editionId ? Number(model.editionId) : null }).then((output) => {
      if (!live) return;
      const list = editionRows(output.features, output.featureValues);
      setRows(list);
      setValues(rowValues(list));
    }).catch(() => { if (live) setRows([]); });
    return () => { live = false; };
  }, [model.editionId]);

  const overrides = rows ? differences(rows, values) : [];
  async function approve(event) {
    event.preventDefault();
    setBusy(true);
    try {
      await callService('TenantRequest', 'Approve', {
        id: request.id, editionId: model.editionId ? Number(model.editionId) : null, isActive: model.isActive,
        subscriptionEndDate: dateInput(model.subscriptionEndDate), reviewNote: model.reviewNote, featureOverrides: overrides
      });
      onApproved();
    } catch (_) { setBusy(false); }
  }

  return <Modal title={L('Approve {0}', request.storeName)} size="wd" onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="request-approve" className="btn" disabled={busy}>{busy ? <><i className="spn" /> {L('Creating the store…')}</> : L('Approve and create the store')}</button>
  </>}>
    <form id="request-approve" onSubmit={approve}>
      <div className="fm hs-fm">
        <label><span>{L('Edition')}</span><select value={model.editionId} onChange={(event) => set('editionId', event.target.value)}>
          <option value="">{L('No edition')}</option>{editions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select></label>
        <label><span>{L('Subscription ends')}</span><input type="date" value={model.subscriptionEndDate} onChange={(event) => set('subscriptionEndDate', event.target.value)} /></label>
        <label className="ck hs-full"><input type="checkbox" checked={model.isActive} onChange={(event) => set('isActive', event.target.checked)} />{L('Active: the store can sign in straight away')}</label>
        <label className="hs-full"><span>{L('Note to the applicant')}</span><textarea value={model.reviewNote} maxLength={1024} placeholder={L('Optional')} onChange={(event) => set('reviewNote', event.target.value)} /></label>
      </div>
      {canSeeFeatures && <details className="hs-more">
        <summary>{L('Features')}{overrides.length ? ` · ${L('{0} changed', overrides.length)}` : ''}</summary>
        <p className="mu" style={{ margin: '4px 0 6px' }}>{L('Start from the edition. Anything you change is saved for this store only.')}</p>
        {rows ? <FeatureTree rows={rows} values={values} overrides onChange={(name, value) => setValues((v) => ({ ...v, [name]: value }))} /> : <Skeleton kpis={false} lines={4} />}
      </details>}
      <p className="hs-note">{L('The store, its administrator ({0}) and the starter data are created, and the administrator gets an email.', request.adminEmailAddress)}</p>
    </form>
  </Modal>;
}

function RejectModal({ request, onClose, onRejected }) {
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  async function reject(event) {
    event.preventDefault();
    if (!reason.trim()) return;
    setBusy(true);
    try { await callService('TenantRequest', 'Reject', { id: request.id, reviewNote: reason.trim() }); onRejected(); } catch (_) { setBusy(false); }
  }
  return <Modal title={L('Reject {0}?', request.storeName)} onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="request-reject" className="btn d" disabled={busy || !reason.trim()}>{busy ? <><i className="spn" /> {L('Rejecting…')}</> : L('Reject')}</button>
  </>}>
    <form id="request-reject" onSubmit={reject} className="fm hs-fm" style={{ gridTemplateColumns: '1fr' }}>
      <label><span>{L('Reason')}<span className="req"> *</span></span><textarea required maxLength={1024} value={reason} onChange={(event) => setReason(event.target.value)} placeholder={L('The applicant sees this in the email and on the status page.')} /></label>
    </form>
  </Modal>;
}
