import React, { useEffect, useState } from 'react';
import { callService } from '../abp.js';
import { HtmlView } from '../template.jsx';
import { notify } from '../toast.js';
import template from './index.html?raw';
import AuditLogsPage from './audit-logs.jsx';
import { RolesPage, UsersPage } from './roles-users.jsx';
import SettingsPage from './settings.jsx';

const pages = {
  account: { title: 'My settings', permission: null },
  roles: { title: 'Roles', permission: 'Pages.Administration.Roles' },
  users: { title: 'Users', permission: 'Pages.Administration.Users' },
  audit: { title: 'Audit logs', permission: 'Pages.Administration.AuditLogs' },
  settings: { title: 'Settings', permission: null },
  maintenance: { title: 'Maintenance', permission: 'Pages.Administration.Host.Maintenance' }
};
const read = (obj, key) => obj?.[key] ?? obj?.[key[0].toUpperCase() + key.slice(1)];

function DataTable({ columns, rows, page, onEdit, onPermissions }) {
  if (!rows.length) return <p className="admin-empty">No records found.</p>;
  return <div className="admin-table-wrap"><table className="admin-table"><thead><tr>{columns.map(([label]) => <th key={label}>{label}</th>)}<th>Actions</th></tr></thead><tbody>{rows.map((row, i) => <tr key={read(row, 'id') ?? i}>{columns.map(([label, key]) => <td key={key}>{key === 'roles' ? (read(row, key) || []).map((role) => read(role, 'roleName')).join(', ') : (read(row, key) ?? '—')}</td>)}<td><div className="admin-row-actions">{page === 'roles' && <><button type="button" className="admin-link-button" onClick={() => onEdit(row)}>Edit role</button><button type="button" className="admin-link-button" onClick={() => onPermissions(row)}>Permissions</button></>}{page === 'users' && <><button type="button" className="admin-link-button" onClick={() => onEdit(row)}>Edit user</button><button type="button" className="admin-link-button" onClick={() => onPermissions(row)}>Permissions</button></>}</div></td></tr>)}</tbody></table></div>;
}

function PermissionChecks({ permissions = [], selected = [], onChange }) {
  const chosen = new Set(selected);
  return <div className="admin-permission-list">{permissions.map((permission) => {
    const name = read(permission, 'name');
    if (!name) return null;
    return <label className="admin-permission-row" key={name}><input type="checkbox" checked={chosen.has(name)} onChange={(event) => onChange(name, event.target.checked)} /><span><strong>{read(permission, 'displayName') || name}</strong><small>{name}</small></span></label>;
  })}</div>;
}

function EditModal({ page, record, mode, onClose, onSaved }) {
  const [model, setModel] = useState(null); const [selected, setSelected] = useState([]); const [busy, setBusy] = useState(true); const [saving, setSaving] = useState(false); const [error, setError] = useState('');
  const id = read(record, 'id');
  useEffect(() => {
    let live = true;
    async function load() {
      setBusy(true); setError('');
      try {
        let result;
        if (mode === 'permissions' && page === 'users') result = await callService('User', 'GetUserPermissionsForEdit', { id });
        else if (page === 'roles') result = await callService('Role', 'GetRoleForEdit', { id });
        else result = await callService('User', 'GetUserForEdit', { id });
        if (!live) return;
        setModel(result);
        setSelected(read(result, 'grantedPermissionNames') || []);
      } catch (e) { if (live) setError(e.message); }
      finally { if (live) setBusy(false); }
    }
    load(); return () => { live = false; };
  }, [id, page, mode]);
  const setField = (field, value) => setModel((old) => ({ ...old, [field]: value }));
  const setNestedField = (field, value) => setModel((old) => ({ ...old, role: { ...read(old, field), displayName: value } }));
  function togglePermission(name, checked) { setSelected((old) => checked ? [...new Set([...old, name])] : old.filter((entry) => entry !== name)); }
  async function save(event) {
    event.preventDefault(); setSaving(true); setError('');
    try {
      if (mode === 'permissions' && page === 'users') await callService('User', 'UpdateUserPermissions', { id, grantedPermissionNames: selected });
      else if (page === 'roles') await callService('Role', 'CreateOrUpdateRole', { role: read(model, 'role'), grantedPermissionNames: selected });
      else {
        const user = read(model, 'user');
        const assignedRoleNames = (read(model, 'roles') || []).filter((role) => read(role, 'isAssigned')).map((role) => read(role, 'roleName'));
        await callService('User', 'CreateOrUpdateUser', { user, assignedRoleNames, sendActivationEmail: false, setRandomPassword: false, tenantId: 0, emailConfirmed: false });
      }
      notify('success', 'Changes saved successfully.'); onSaved(); onClose();
    } catch (e) { setError(e.message); } finally { setSaving(false); }
  }
  async function resetUserPermissions() {
    setSaving(true); setError('');
    try { await callService('User', 'ResetUserSpecificPermissions', { id }); notify('success', 'User-specific permissions reset to role defaults.'); onSaved(); onClose(); }
    catch (e) { setError(e.message); } finally { setSaving(false); }
  }
  const permissions = read(model, 'permissions') || [];
  const modalTitle = page === 'roles' ? `Edit role: ${read(read(model, 'role') || {}, 'displayName') || ''}` : mode === 'permissions' ? `User permissions: ${read(record, 'userName') || ''}` : `Edit user: ${read(read(model, 'user') || {}, 'userName') || ''}`;
  return <div className="admin-modal-backdrop" onMouseDown={(event) => event.target === event.currentTarget && onClose()}><form className="admin-modal" onSubmit={save}><div className="admin-modal-heading"><div><span className="stock-kicker">Administration</span><h2>{modalTitle}</h2></div><button type="button" className="modal-close" onClick={onClose} aria-label="Close">×</button></div>
    {error && <div className="admin-error">{error}</div>}{busy ? <p className="admin-empty">Loading…</p> : page === 'roles' ? <><label className="admin-modal-field">Display name<input required value={read(read(model, 'role') || {}, 'displayName') || ''} onChange={(e) => setNestedField('role', e.target.value)} /></label><label className="admin-check-field"><input type="checkbox" checked={!!read(read(model, 'role') || {}, 'isDefault')} onChange={(e) => setModel((old) => ({ ...old, role: { ...read(old, 'role'), isDefault: e.target.checked } }))} /> Default role</label><h3>Role permissions</h3><PermissionChecks permissions={permissions} selected={selected} onChange={togglePermission} /></> : mode === 'permissions' ? <><p className="admin-modal-hint">These are the user's effective permissions. Saving sets user-level grants.</p><PermissionChecks permissions={permissions} selected={selected} onChange={togglePermission} /></> : <><div className="admin-user-fields">{[['Name', 'name'], ['Surname', 'surname'], ['Username', 'userName'], ['Email', 'emailAddress'], ['Mobile', 'phoneNumber']].map(([label, field]) => <label className="admin-modal-field" key={field}>{label}<input required={['name', 'surname', 'userName', 'emailAddress'].includes(field)} type={field === 'emailAddress' ? 'email' : 'text'} value={read(read(model, 'user') || {}, field) || ''} onChange={(e) => setModel((old) => ({ ...old, user: { ...read(old, 'user'), [field]: e.target.value } }))} /></label>)}</div><label className="admin-check-field"><input type="checkbox" checked={!!read(read(model, 'user') || {}, 'isActive')} onChange={(e) => setModel((old) => ({ ...old, user: { ...read(old, 'user'), isActive: e.target.checked } }))} /> Active</label><h3>Assigned roles</h3><div className="admin-permission-list">{(read(model, 'roles') || []).map((role) => <label className="admin-permission-row" key={read(role, 'roleId')}><input type="checkbox" checked={!!read(role, 'isAssigned')} onChange={(e) => setModel((old) => ({ ...old, roles: read(old, 'roles').map((item) => read(item, 'roleId') === read(role, 'roleId') ? { ...item, isAssigned: e.target.checked } : item) }))} /><span><strong>{read(role, 'roleDisplayName') || read(role, 'roleName')}</strong></span></label>)}</div></>}
    <div className="admin-modal-footer">{mode === 'permissions' && page === 'users' && <button type="button" className="button button-secondary admin-reset-button" disabled={busy || saving} onClick={resetUserPermissions}>Reset to role defaults</button>}<button type="button" className="button button-secondary" onClick={onClose}>Cancel</button><button type="submit" className="button button-primary" disabled={busy || saving}>{saving ? 'Saving…' : 'Save changes'}</button></div>
  </form></div>;
}

function RecordsPage({ page }) {
  const [data, setData] = useState([]); const [filter, setFilter] = useState(''); const [busy, setBusy] = useState(true); const [error, setError] = useState('');
  const [editing, setEditing] = useState(null);
  async function refresh() {
    setBusy(true); setError('');
    try {
      const result = page === 'roles' ? await callService('Role', 'GetRoles', {}) : await callService('User', 'GetUsers', { filter, maxResultCount: 50, skipCount: 0 });
      setData(read(result, 'items') || []);
    } catch (e) { setError(e.message); } finally { setBusy(false); }
  }
  useEffect(() => { refresh(); }, [page]);
  const columns = page === 'roles' ? [['Name', 'displayName'], ['Code', 'name'], ['Default', 'isDefault'], ['Created', 'creationTime']] : [['Name', 'name'], ['Username', 'userName'], ['Email', 'emailAddress'], ['Mobile', 'phoneNumber'], ['Roles', 'roles'], ['Active', 'isActive']];
  return <><div className="admin-page-heading"><div><h1>{pages[page].title}</h1><p>{`Manage ${pages[page].title.toLowerCase()} for this workspace.`}</p></div><div className="admin-toolbar">{page !== 'roles' && <input aria-label="Filter" placeholder="Search users" value={filter} onChange={(e) => setFilter(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && refresh()} />}<button className="button button-secondary" onClick={refresh}>Refresh</button></div></div>{error && <div className="admin-error">{error}</div>}{busy ? <p className="admin-empty">Loading…</p> : <DataTable columns={columns} rows={data} page={page} onEdit={(row) => setEditing({ row, mode: 'edit' })} onPermissions={(row) => setEditing({ row, mode: 'permissions' })} />}{editing && <EditModal page={page} record={editing.row} mode={editing.mode} onClose={() => setEditing(null)} onSaved={refresh} />}</>;
}

function MaintenancePage() {
  const [caches, setCaches] = useState([]); const [message, setMessage] = useState(''); const [error, setError] = useState('');
  async function load() { setError(''); try { const result = await callService('Caching', 'GetAllCaches'); setCaches(read(result, 'items') || []); } catch(e) { setError(e.message); } }
  useEffect(() => { load(); }, []);
  async function clearAll() { setMessage(''); setError(''); try { await callService('Caching', 'ClearAllCaches'); setMessage('All application caches were cleared.'); notify('info', 'All application caches were cleared.'); } catch(e) { setError(e.message); } }
  async function clear(name) { setMessage(''); setError(''); try { await callService('Caching', 'ClearCache', { id: name }); setMessage(`${name} cache was cleared.`); notify('success', `${name} cache was cleared.`); } catch(e) { setError(e.message); } }
  return <><div className="admin-page-heading"><div><h1>Maintenance</h1><p>Clear application caches for the current host.</p></div><button className="button button-primary" onClick={clearAll}>Clear all caches</button></div>{error && <div className="admin-error">{error}</div>}{message && <div className="admin-success">{message}</div>}<div className="admin-cache-list">{caches.map((cache) => { const name = read(cache, 'name'); return <div key={name}><strong>{name}</strong><button className="button button-secondary" onClick={() => clear(name)}>Clear</button></div>; })}{!caches.length && <p className="admin-empty">No cache entries available, or your account does not have host maintenance access.</p>}</div></>;
}

function AdministrationContent({ page, permissions = [] }) {
  const definition = pages[page];
  if (!definition) return null;
  if (page === 'account') {
    const section = new URLSearchParams(window.location.hash.split('?')[1] || '').get('section') || 'my-settings';
    const labels = { 'linked-accounts': 'Linked accounts', 'change-password': 'Change password', 'login-attempts': 'Login attempts', 'profile-picture': 'Change profile picture', 'my-settings': 'My settings' };
    return <><div className="admin-page-heading"><div><h1>{labels[section] || 'My settings'}</h1><p>Manage your StockPilot account.</p></div></div><section className="panel account-option-panel"><strong>{labels[section] || 'My settings'}</strong><p>This account option is available from your authenticated StockPilot account.</p></section></>;
  }
  if (definition.permission && !permissions.includes(definition.permission)) return <section className="panel section-placeholder"><h1>Access denied</h1><p>Your account does not have permission to view this page.</p></section>;
  if (page === 'settings') {
    if (!permissions.includes('Pages.Administration.Tenant.Settings') && !permissions.includes('Pages.Administration.Host.Settings')) return <section className="panel section-placeholder"><h1>Access denied</h1><p>Your account does not have permission to view settings.</p></section>;
    return <SettingsPage />;
  }
  if (page === 'maintenance') return <MaintenancePage />;
  if (page === 'audit') return <AuditLogsPage />;
  if (page === 'roles') return <RolesPage />;
  if (page === 'users') return <UsersPage />;
  return <RecordsPage page={page} />;
}

export default function AdministrationPage(props) {
  return <HtmlView template={template}><AdministrationContent {...props} /></HtmlView>;
}

