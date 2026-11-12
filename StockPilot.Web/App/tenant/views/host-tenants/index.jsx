import { useEffect, useState } from 'react';
import { callService, isGranted, L, notifySuccess } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import Drawer, { DrawerHeader } from '../inventory-shared/drawer.jsx';
import { date, day } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import Modal from '../inventory-shared/modal.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { dateInput, daysLeftText, FeatureTree, rowValues, SubscriptionDate, SUBSCRIPTION, Tabs, tenantRows, UsageBar, useEditions } from '../host-shared/index.jsx';

const loadEditions = () => callService('Edition', 'GetEditionComboboxItems', 0)
  .then((items) => [['0', L('No edition')], ...(items || []).filter((i) => i.value && i.value !== 'null').map((i) => [i.value, i.displayText])]);

// A function, so the option labels are translated when the page opens (after the texts are loaded).
const filters = () => [
  { key: 'isActive', all: 'All stores', options: [['true', L('Active')], ['false', L('Inactive')]], map: (v) => v === 'true' },
  { key: 'editionId', all: 'All editions', load: loadEditions, number: true },
  { key: 'subscriptionStatus', all: 'Any subscription', options: [[String(SUBSCRIPTION.Active), L('Active')], [String(SUBSCRIPTION.ExpiringSoon), L('Ends soon')], [String(SUBSCRIPTION.Expired), L('Expired')], [String(SUBSCRIPTION.None), L('No end date')]], number: true },
  { key: 'hasCustomFeatures', all: 'Any features', options: [['true', L('Custom features')], ['false', L('Edition features')]], map: (v) => v === 'true' }
];

const COLUMNS = [
  { key: 'tenancyName', label: 'Store', render: (t) => <div className="nm"><div><b>{t.name}{t.featureOverrideCount > 0 && <span className="ch hs-custom" title={t.featureOverrideCount === 1 ? L('1 feature differs from the edition') : L('{0} features differ from the edition', t.featureOverrideCount)}>{L('Custom features')}</span>}</b><span>{t.tenancyName}</span></div></div> },
  { key: 'editionDisplayName', label: 'Edition', sortKey: 'EditionId', render: (t) => t.editionDisplayName || <span className="mu">{L('No edition')}</span> },
  { key: 'contactName', label: 'Contact', render: (t) => <div className="nm"><div><b>{t.contactName || '—'}</b><span>{t.contactPhone}</span></div></div> },
  { key: 'userCount', label: 'Users', right: true, nosort: true },
  { key: 'subscriptionEndDate', label: 'Subscription ends', render: (t) => <SubscriptionDate value={t.subscriptionEndDate} status={t.subscriptionStatus} />, text: (t) => date(t.subscriptionEndDate) },
  { key: 'isActive', label: 'Status', render: (t) => <span className={`b ${t.isActive ? 'OK' : 'hc'}`}>{L(t.isActive ? 'Active' : 'Inactive')}</span> },
  { key: 'creationTime', label: 'Created', text: (t) => date(t.creationTime) }
];

/**
 * Stores (Tenant): filters, the grid, Add store, and a drawer per store with Details (edit, activate, extend,
 * unlock admin, delete), Features (edition defaults and this store's overrides) and Usage.
 */
export default function HostTenantsPage() {
  const [reload, setReload] = useState(0);
  const [openId, setOpenId] = useState(null);
  const [adding, setAdding] = useState(false);
  useHashQuery((query) => {
    if (query.get('open')) setOpenId(Number(query.get('open')));
    if (query.get('new') && isGranted('Pages.Tenants.Create')) setAdding(true);
  });
  const changed = () => setReload((k) => k + 1);

  return <>
    <div className="hd">
      <div><h1>{L('Stores')}</h1><p className="sub">{L('Every store, its edition, subscription and features.')}</p></div>
      {isGranted('Pages.Tenants.Create') && <div className="ac"><button className="btn l" onClick={() => setAdding(true)}><Icon name="plus" size={17} />{L('Add store')}</button></div>}
    </div>
    <AbpGrid service="Tenant" method="GetTenants" columns={COLUMNS} filters={filters()} defaultSorting="TenancyName" reloadKey={reload}
      rowClass={(t) => (t.isActive ? '' : 'mu')} onRowClick={(row) => setOpenId(row.id)} />
    {openId && <TenantDrawer id={openId} onClose={() => setOpenId(null)} onChanged={changed} />}
    {adding && <TenantFormModal onClose={() => setAdding(false)} onSaved={() => { setAdding(false); notifySuccess(L('Store created.')); changed(); }} />}
  </>;
}

function TenantDrawer({ id, onClose, onChanged }) {
  const [tenant, setTenant] = useState(null);
  const [tab, setTab] = useState('details');
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let live = true;
    callService('Tenant', 'GetTenantForEdit', { id }).then((t) => { if (live) setTenant(t); }).catch(() => onClose());
    return () => { live = false; };
  }, [id, reload]);

  const refresh = () => { setReload((k) => k + 1); onChanged(); };
  if (!tenant) return <Drawer onClose={onClose}><DrawerHeader title={L('Store')} onClose={onClose} /><div className="db"><Skeleton kpis={false} /></div></Drawer>;

  const tabs = [['details', 'Details'], ...(isGranted('Pages.Tenants.ChangeFeatures') ? [['features', 'Features']] : []), ['usage', 'Usage']];
  return <Drawer onClose={onClose}>
    <DrawerHeader title={tenant.name} subtitle={`${tenant.tenancyName} · ${tenant.editionDisplayName || L('No edition')}`}
      badge={<span className={`b ${tenant.isActive ? 'OK' : 'hc'}`}>{L(tenant.isActive ? 'Active' : 'Inactive')}</span>} onClose={onClose} />
    <div className="hs-tabs"><Tabs tabs={tabs} value={tab} onChange={setTab} /></div>
    <div className="db">
      {tab === 'details' && <DetailsTab tenant={tenant} onChanged={refresh} onDeleted={() => { onChanged(); onClose(); }} />}
      {tab === 'features' && <FeaturesTab tenant={tenant} onChanged={refresh} />}
      {tab === 'usage' && <UsageTab tenant={tenant} />}
    </div>
  </Drawer>;
}

function DetailsTab({ tenant, onChanged, onDeleted }) {
  const [dialog, setDialog] = useState('');
  const canEdit = isGranted('Pages.Tenants.Edit');
  const field = (label, value) => <div><span>{L(label)}</span>{value || '—'}</div>;
  const done = (message) => { setDialog(''); notifySuccess(message); onChanged(); };

  async function setActive() {
    await callService('Tenant', 'SetActive', { id: tenant.id, isActive: !tenant.isActive });
    done(L(tenant.isActive ? 'Store deactivated. Its users cannot sign in.' : 'Store activated.'));
  }
  async function unlock() {
    await callService('Tenant', 'UnlockTenantAdmin', { id: tenant.id });
    done(L('The administrator account is unlocked.'));
  }

  return <>
    <div className="dl">
      {field('Subscription ends', tenant.subscriptionEndDate ? <span className="hs-inline"><SubscriptionDate value={tenant.subscriptionEndDate} status={tenant.subscriptionStatus} /> <span className="mu">· {daysLeftText(tenant.subscriptionEndDate)}</span></span> : L('No end date'))}
      {field('Created', date(tenant.creationTime))}
      {field('Users', String(tenant.userCount))}
      {field('Features', tenant.featureOverrideCount ? (tenant.featureOverrideCount === 1 ? L('1 feature differs from the edition') : L('{0} differ from the edition', tenant.featureOverrideCount)) : L('As the edition'))}
      {field('Contact person', tenant.contactName)}{field('Contact phone', tenant.contactPhone)}
      {field('Contact email', tenant.contactEmail)}{field('Business type', tenant.businessType)}
      {field('Address', tenant.address)}{field('City', [tenant.city, tenant.countryName].filter(Boolean).join(', '))}
    </div>
    {tenant.notes && <p className="mu" style={{ whiteSpace: 'pre-wrap' }}>{tenant.notes}</p>}
    {canEdit && <div className="hs-actions">
      <button className="btn s" onClick={() => setDialog('edit')}><Icon name="edit" size={15} />{L('Edit')}</button>
      <button className="btn s" onClick={() => setDialog('extend')}>{L('Extend subscription')}</button>
      <button className="btn s" onClick={() => setDialog('active')}>{L(tenant.isActive ? 'Deactivate' : 'Activate')}</button>
      <button className="btn s" onClick={() => setDialog('unlock')}><Icon name="lock" size={15} />{L('Unlock admin')}</button>
    </div>}
    {isGranted('Pages.Tenants.Delete') && <div className="hs-danger">
      <button className="btn s d" onClick={() => setDialog('delete')}><Icon name="trash" size={15} />{L('Delete store')}</button>
    </div>}

    {dialog === 'edit' && <TenantFormModal tenant={tenant} onClose={() => setDialog('')} onSaved={() => done(L('Store saved.'))} />}
    {dialog === 'extend' && <ExtendModal tenant={tenant} onClose={() => setDialog('')} onSaved={() => done(L('Subscription extended.'))} />}
    {dialog === 'active' && <ConfirmDialog title={L(tenant.isActive ? 'Deactivate {0}?' : 'Activate {0}?', tenant.name)} okText={L(tenant.isActive ? 'Deactivate' : 'Activate')}
      okClass={tenant.isActive ? 'd' : ''} onConfirm={setActive} onClose={() => setDialog('')}>
      <p style={{ margin: 0 }}>{L(tenant.isActive ? 'Nobody in this store can sign in until you activate it again. Its data is kept.' : 'The store’s users can sign in again.')}</p>
    </ConfirmDialog>}
    {dialog === 'unlock' && <ConfirmDialog title={L('Unlock the administrator?')} okText={L('Unlock')} onConfirm={unlock} onClose={() => setDialog('')}>
      <p style={{ margin: 0 }}>{L('Clears failed sign-in lockouts on the store’s admin account.')}</p>
    </ConfirmDialog>}
    {dialog === 'delete' && <DeleteModal tenant={tenant} onClose={() => setDialog('')} onDeleted={() => { setDialog(''); notifySuccess(L('Store deleted.')); onDeleted(); }} />}
  </>;
}

function DeleteModal({ tenant, onClose, onDeleted }) {
  const [typed, setTyped] = useState('');
  const matches = typed.trim().toLowerCase() === tenant.tenancyName.toLowerCase();
  return <ConfirmDialog title={L('Delete {0}?', tenant.name)} okText={L('Delete store')} okClass="d" okDisabled={!matches} busyText={L('Deleting…')} onClose={onClose}
    onConfirm={async () => { await callService('Tenant', 'DeleteTenant', { id: tenant.id }); onDeleted(); }}>
    <p style={{ marginTop: 0 }}>{L('The store and everyone in it lose access. Type the workspace name {0} to confirm.', tenant.tenancyName)}</p>
    <input style={{ width: '100%' }} value={typed} autoComplete="off" aria-label={L('Workspace name')} placeholder={tenant.tenancyName} onChange={(event) => setTyped(event.target.value)} />
    {typed && !matches && <div className="warn" style={{ marginTop: 6 }}>{L('The name does not match.')}</div>}
  </ConfirmDialog>;
}

function addMonths(from, months) {
  const d = new Date(from || day(0));
  d.setMonth(d.getMonth() + months);
  return d.toISOString().slice(0, 10);
}

function ExtendModal({ tenant, onClose, onSaved }) {
  // From the current end date when it is still ahead, otherwise from today.
  const base = tenant.subscriptionEndDate && date(tenant.subscriptionEndDate) > day(0) ? date(tenant.subscriptionEndDate) : day(0);
  const [value, setValue] = useState(addMonths(base, 12));
  const [busy, setBusy] = useState(false);
  async function save() {
    setBusy(true);
    try { await callService('Tenant', 'ExtendSubscription', { id: tenant.id, newEndDate: dateInput(value) }); onSaved(); } catch (_) { setBusy(false); }
  }
  return <Modal title={L('Extend subscription')} onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="button" className="btn" disabled={busy || !value} onClick={save}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Extend')}</button>
  </>}>
    <p className="mu" style={{ marginTop: 0 }}>{L('Ends now: {0}', tenant.subscriptionEndDate ? date(tenant.subscriptionEndDate) : L('No end date'))}</p>
    <div className="pills">{[[1, '+1 month'], [3, '+3 months'], [6, '+6 months'], [12, '+1 year']].map(([months, label]) =>
      <button type="button" key={months} className={value === addMonths(base, months) ? 'on' : ''} onClick={() => setValue(addMonths(base, months))}>{L(label)}</button>)}</div>
    <label className="fl"><span>{L('New end date')}</span><input type="date" value={value} onChange={(event) => setValue(event.target.value)} /></label>
  </Modal>;
}

const BUSINESS_FIELDS = [
  ['contactName', 'Contact person'], ['contactPhone', 'Contact phone'], ['contactEmail', 'Contact email', 'email'], ['businessType', 'Business type'],
  ['address', 'Address'], ['city', 'City'], ['countryName', 'Country']
];

/** Add store (CreateTenant, with its administrator) and Edit store (UpdateTenant) share this form. */
function TenantFormModal({ tenant, onClose, onSaved }) {
  const editing = !!tenant;
  const editions = useEditions();
  const [model, setModel] = useState(() => (editing
    ? { ...tenant, editionId: tenant.editionId ? String(tenant.editionId) : '', subscriptionEndDate: date(tenant.subscriptionEndDate) }
    : { tenancyName: '', name: '', editionId: '', isActive: true, subscriptionEndDate: day(365), adminEmailAddress: '', adminName: '', adminSurname: '', adminPhoneNumber: '', adminPassword: '', shouldChangePasswordOnNextLogin: true, sendActivationEmail: true }));
  const [busy, setBusy] = useState(false);
  const set = (key, value) => setModel((m) => ({ ...m, [key]: value }));
  useEffect(() => { if (!editing && !model.editionId && editions.length) set('editionId', editions[0][0]); }, [editions.length]);

  async function save(event) {
    event.preventDefault();
    setBusy(true);
    const input = { ...model, editionId: model.editionId ? Number(model.editionId) : null, subscriptionEndDate: dateInput(model.subscriptionEndDate) };
    try {
      await callService('Tenant', editing ? 'UpdateTenant' : 'CreateTenant', input);
      onSaved();
    } catch (_) { setBusy(false); }
  }

  const text = (key, label, type = 'text', required = false, extra = {}) => <label key={key} className={extra.full ? 'hs-full' : undefined}>
    <span>{L(label)}{required && <span className="req"> *</span>}</span>
    <input type={type} required={required} value={model[key] ?? ''} onChange={(event) => set(key, event.target.value)} {...extra.input} />
  </label>;

  return <Modal title={editing ? L('Edit {0}', tenant.name) : L('Add store')} size="wd" onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="tenant-form" className="btn" disabled={busy}>{busy ? <><i className="spn" /> {L('Saving…')}</> : editing ? L('Save') : L('Create store')}</button>
  </>}>
    <form id="tenant-form" onSubmit={save}>
      <div className="fm hs-fm">
        {text('name', 'Store name', 'text', true)}
        {text('tenancyName', 'Workspace name', 'text', true, { input: { pattern: '[A-Za-z][A-Za-z0-9_\\-]*', maxLength: 64 } })}
        <label><span>{L('Edition')}</span><select value={model.editionId} onChange={(event) => set('editionId', event.target.value)}>
          <option value="">{L('No edition')}</option>{editions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select></label>
        {text('subscriptionEndDate', 'Subscription ends', 'date')}
        <label className="ck hs-full"><input type="checkbox" checked={!!model.isActive} onChange={(event) => set('isActive', event.target.checked)} />{L('Active')}</label>
      </div>
      {!editing && <><div className="hs-section">{L('Administrator account')}</div>
        <div className="fm hs-fm">
          {text('adminName', 'First name')}{text('adminSurname', 'Last name')}
          {text('adminEmailAddress', 'Email', 'email', true)}{text('adminPhoneNumber', 'Mobile number', 'tel')}
          {text('adminPassword', 'Password', 'password', false, { input: { autoComplete: 'new-password', placeholder: L('Leave empty to generate one') } })}
          <label className="ck"><input type="checkbox" checked={model.shouldChangePasswordOnNextLogin} onChange={(event) => set('shouldChangePasswordOnNextLogin', event.target.checked)} />{L('Change password at first sign-in')}</label>
          <label className="ck hs-full"><input type="checkbox" checked={model.sendActivationEmail} onChange={(event) => set('sendActivationEmail', event.target.checked)} />{L('Email the administrator')}</label>
        </div></>}
      <details className="hs-more" open={editing}>
        <summary>{L('Business details')}</summary>
        <div className="fm hs-fm" style={{ paddingBottom: 12 }}>
          {BUSINESS_FIELDS.map(([key, label, type]) => text(key, label, type || 'text'))}
          <label className="hs-full"><span>{L('Notes')}</span><textarea value={model.notes ?? ''} onChange={(event) => set('notes', event.target.value)} /></label>
        </div>
      </details>
    </form>
  </Modal>;
}

function FeaturesTab({ tenant, onChanged }) {
  const [data, setData] = useState(null);
  const [usage, setUsage] = useState(null);
  const [values, setValues] = useState({});
  const [saved, setSaved] = useState({});
  const [busy, setBusy] = useState(false);
  const [resetting, setResetting] = useState(false);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let live = true;
    Promise.all([callService('Tenant', 'GetTenantFeaturesForEdit', { id: tenant.id }), callService('Tenant', 'GetFeatureUsage', { id: tenant.id })]).then(([output, used]) => {
      if (!live) return;
      const rows = tenantRows(output.features);
      setData({ ...output, rows });
      setValues(rowValues(rows));
      setSaved(rowValues(rows));
      setUsage(used);
    }).catch(() => { if (live) setData(false); });
    return () => { live = false; };
  }, [tenant.id, reload]);

  if (data === null) return <Skeleton kpis={false} lines={6} />;
  if (data === false) return <div className="empty"><b>{L('Something went wrong')}</b></div>;

  const changes = data.rows.filter((r) => String(values[r.name]) !== String(saved[r.name])).map((r) => ({ name: r.name, value: String(values[r.name]) }));
  const overridden = data.rows.filter((r) => r.isOverridden).length;
  async function save() {
    setBusy(true);
    try {
      await callService('Tenant', 'UpdateTenantFeatures', { id: tenant.id, featureValues: changes });
      notifySuccess(L('Features saved. The store sees them on its next page load.'));
      setReload((k) => k + 1); onChanged();
    } catch (_) { /* shown by abp.ajax */ } finally { setBusy(false); }
  }

  return <>
    <p className="mu" style={{ marginTop: 0 }}>{L('Edition: {0}. Values that differ from the edition are this store’s own.', data.editionDisplayName || L('No edition'))}</p>
    <FeatureTree rows={data.rows} values={values} overrides usage={usage} onChange={(name, value) => setValues((v) => ({ ...v, [name]: value }))} />
    <p className="hs-note">{L('Changes apply when the store’s users next load a page. A limit of 0 means no limit.')}</p>
    <div className="hs-actions" style={{ justifyContent: 'space-between', marginTop: 14 }}>
      <button className="btn s d" disabled={!overridden || busy} onClick={() => setResetting(true)}>{L('Reset all to edition')}</button>
      <div className="hs-actions">
        <button className="btn s" disabled={!changes.length || busy} onClick={() => setValues(saved)}>{L('Undo changes')}</button>
        <button className="btn" disabled={!changes.length || busy} onClick={save}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save features')}</button>
      </div>
    </div>
    {resetting && <ConfirmDialog title={L('Reset all features?')} okText={L('Reset')} okClass="d" busyText={L('Resetting…')} onClose={() => setResetting(false)}
      onConfirm={async () => { await callService('Tenant', 'ResetTenantSpecificFeatures', { id: tenant.id }); notifySuccess(L('Features reset to the edition.')); setReload((k) => k + 1); onChanged(); }}>
      <p style={{ margin: 0 }}>{L('{0} store-specific values are removed and the store uses its edition’s features again.', overridden)}</p>
    </ConfirmDialog>}
  </>;
}

function UsageTab({ tenant }) {
  const [usage, setUsage] = useState(null);
  useEffect(() => { callService('Tenant', 'GetFeatureUsage', { id: tenant.id }).then(setUsage).catch(() => setUsage(false)); }, [tenant.id]);
  if (usage === null) return <Skeleton kpis={false} lines={3} />;
  if (usage === false) return <div className="empty"><b>{L('Something went wrong')}</b></div>;
  return <>
    <UsageBar label={L('Users')} used={usage.userCount} limit={usage.maxUserCount} />
    <UsageBar label={L('Active items')} used={usage.activeItemCount} limit={usage.maxItemCount} />
    <p className="mu">{L('Amber from 80% of a limit, red when it is reached. Limits are set on the Features tab.')}</p>
  </>;
}
