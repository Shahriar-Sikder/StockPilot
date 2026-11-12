import { useEffect, useMemo, useState } from 'react';
import { callService, isAnyGranted, isGranted, L, notifySuccess } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import { date } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import Modal from '../inventory-shared/modal.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { Tabs } from '../host-shared/index.jsx';
import PermissionTree from './permission-tree.jsx';

const sameSet = (a, b) => a.size === b.size && [...a].every((x) => b.has(x));

/* ---------- Roles ---------- */

/** Roles: list, Add role, and an editor with the role's name and its permission tree. */
export function RolesPage() {
  const [rows, setRows] = useState(null);
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState(null); // {} = new, { id } = edit
  const [deleting, setDeleting] = useState(null);
  const canEdit = isGranted('Pages.Administration.Roles.Edit');
  const canDelete = isGranted('Pages.Administration.Roles.Delete');

  useEffect(() => { callService('Role', 'GetRoles', {}).then((r) => setRows(r.items || [])).catch(() => setRows([])); }, [reload]);
  const changed = () => setReload((k) => k + 1);

  const columns = [
    { key: 'displayName', label: 'Role', render: (r) => <div className="nm"><div><b>{r.displayName}</b><span>{r.name}</span></div></div> },
    { key: 'isDefault', label: 'Type', text: (r) => (r.isStatic ? L('System') : L('Custom')), render: (r) => <div className="au-tags">
      {r.isStatic && <span className="ch">{L('System')}</span>}
      {r.isDefault && <span className="b OK" title={L('New users get this role')}>{L('Default')}</span>}
      {!r.isStatic && !r.isDefault && <span className="mu">{L('Custom')}</span>}
    </div> },
    { key: 'creationTime', label: 'Created', text: (r) => date(r.creationTime) },
    ...(canDelete ? [{ key: 'actions', label: '', nosort: true, stop: true, right: true, render: (r) => (r.isStatic ? null
      : <button className="lnk" onClick={() => setDeleting(r)}><Icon name="trash" size={14} />{L('Delete')}</button>) }] : [])
  ];

  return <>
    <div className="hd">
      <div><h1>{L('Roles')}</h1><p className="sub">{L('A role is a set of permissions. Give users roles, then adjust a single user on the Users page if needed.')}</p></div>
      {isGranted('Pages.Administration.Roles.Create') && <div className="ac"><button className="btn l" onClick={() => setEditing({})}><Icon name="plus" size={17} />{L('Add role')}</button></div>}
    </div>
    <AbpGrid rows={rows} columns={columns} onRowClick={canEdit ? (r) => setEditing({ id: r.id }) : undefined} />
    {editing && <RoleModal id={editing.id} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); changed(); }} />}
    {deleting && <ConfirmDialog title={L('Delete {0}?', deleting.displayName)} okText={L('Delete')} okClass="d" busyText={L('Deleting…')} onClose={() => setDeleting(null)}
      onConfirm={async () => { await callService('Role', 'DeleteRole', { id: deleting.id }); notifySuccess(L('Role deleted.')); changed(); }}>
      <p style={{ margin: 0 }}>{L('Users with this role lose the permissions it gives (their own extra permissions stay).')}</p>
    </ConfirmDialog>}
  </>;
}

function RoleModal({ id, onClose, onSaved }) {
  const [data, setData] = useState(null);
  const [name, setName] = useState('');
  const [isDefault, setIsDefault] = useState(false);
  const [selected, setSelected] = useState(new Set());
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    callService('Role', 'GetRoleForEdit', { id: id ?? null }).then((r) => {
      setData(r);
      setName(r.role?.displayName || '');
      setIsDefault(!!r.role?.isDefault);
      setSelected(new Set(r.grantedPermissionNames || []));
    }).catch(() => onClose());
  }, [id]);

  async function save(event) {
    event.preventDefault();
    if (!name.trim()) return;
    setBusy(true);
    try {
      await callService('Role', 'CreateOrUpdateRole', { role: { id: id ?? null, displayName: name.trim(), isDefault }, grantedPermissionNames: [...selected] });
      notifySuccess(id ? L('Role saved. Its users get the change when they next load a page.') : L('Role created.'));
      onSaved();
    } catch (_) { setBusy(false); }
  }

  return <Modal title={id ? L('Edit role') : L('Add role')} size="xl" onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="role-form" className="btn" disabled={busy || !data}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save')}</button>
  </>}>
    {!data ? <Skeleton kpis={false} lines={8} /> : <form id="role-form" onSubmit={save}>
      <div className="au-form">
        <label><span>{L('Role name')}<span className="req"> *</span></span><input required maxLength={64} value={name} onChange={(e) => setName(e.target.value)} /></label>
        <label className="ck" style={{ alignSelf: 'end', minHeight: 38 }}><input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} />{L('Default role: new users get it')}</label>
      </div>
      <div className="hs-section">{L('Permissions')}</div>
      <PermissionTree permissions={data.permissions || []} selected={selected} onChange={setSelected} grantable={data.grantablePermissionNames ? new Set(data.grantablePermissionNames) : null} />
    </form>}
  </Modal>;
}

/* ---------- Users ---------- */

/** Users: grid with a role filter, Add user, and an editor with Details, Roles and the user's own permission tree. */
export function UsersPage() {
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState(null); // {} = new, { id } = edit
  const canOpen = isAnyGranted('Pages.Administration.Users.Edit', 'Pages.Administration.Users.ChangePermissions');
  useHashQuery((q) => { if (q.get('new') && isGranted('Pages.Administration.Users.Create')) setEditing({}); });
  const changed = () => setReload((k) => k + 1);

  const filters = useMemo(() => (isGranted('Pages.Administration.Roles') ? [{
    key: 'role', all: 'All roles', number: true,
    load: () => callService('Role', 'GetRoles', {}).then((r) => (r.items || []).map((x) => [String(x.id), x.displayName]))
  }] : []), []);

  const columns = [
    { key: 'name', label: 'Name', render: (u) => <div className="nm"><div><b>{[u.name, u.surname].filter(Boolean).join(' ')}</b><span>{u.userName}</span></div></div> },
    { key: 'emailAddress', label: 'Email' },
    { key: 'roles', label: 'Roles', nosort: true, text: (u) => (u.roles || []).map((r) => r.roleName).join(', '),
      render: (u) => <div className="au-tags">{(u.roles || []).map((r) => <span key={r.roleId} className="ch">{r.roleName}</span>)}</div> },
    { key: 'isActive', label: 'Status', render: (u) => <span className={`b ${u.isActive ? 'OK' : 'hc'}`}>{L(u.isActive ? 'Active' : 'Inactive')}</span> },
    { key: 'lastLoginTime', label: 'Last sign-in', text: (u) => date(u.lastLoginTime) || '—' }
  ];

  return <>
    <div className="hd">
      <div><h1>{L('Users')}</h1><p className="sub">{L('People who can sign in. Their roles give most permissions; you can add or remove single permissions for one user.')}</p></div>
      {isGranted('Pages.Administration.Users.Create') && <div className="ac"><button className="btn l" onClick={() => setEditing({})}><Icon name="plus" size={17} />{L('Add user')}</button></div>}
    </div>
    <AbpGrid service="User" method="GetUsers" columns={columns} filters={filters} reloadKey={reload} onRowClick={canOpen ? (u) => setEditing({ id: u.id }) : undefined} />
    {editing && <UserModal id={editing.id} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); changed(); }} />}
  </>;
}

const USER_FIELDS = [['name', 'First name', true], ['surname', 'Last name', true], ['userName', 'User name', true], ['emailAddress', 'Email', true, 'email'], ['phoneNumber', 'Mobile number', false, 'tel']];

function UserModal({ id, onClose, onSaved }) {
  const editing = id != null;
  const canDetails = isGranted(editing ? 'Pages.Administration.Users.Edit' : 'Pages.Administration.Users.Create');
  const canPermissions = editing && isGranted('Pages.Administration.Users.ChangePermissions');
  const [tab, setTab] = useState(canDetails ? 'details' : 'permissions');
  const [details, setDetails] = useState(null);
  const [user, setUser] = useState(null);
  const [roles, setRoles] = useState([]);
  const [sendEmail, setSendEmail] = useState(true);
  const [perms, setPerms] = useState(null);
  const [selected, setSelected] = useState(new Set());
  const [busy, setBusy] = useState(false);
  const [dialog, setDialog] = useState('');
  const [dirty, setDirty] = useState(false);
  // Your own account: roles locked, and only permissions you hold can change (the server checks the same).
  const ownAccount = !!details?.isOwnAccount || !!perms?.isOwnAccount;
  const grantable = useMemo(() => (perms?.grantablePermissionNames ? new Set(perms.grantablePermissionNames) : null), [perms]);

  useEffect(() => {
    if (canDetails) callService('User', 'GetUserForEdit', { id: id ?? null }).then((r) => {
      setDetails(r);
      setUser({ ...r.user, password: '', isActive: editing ? r.user.isActive : true, shouldChangePasswordOnNextLogin: editing ? r.user.shouldChangePasswordOnNextLogin : true });
      setRoles(r.roles || []);
    }).catch(() => onClose());
    if (canPermissions) loadPermissions();
  }, [id]);

  function loadPermissions() {
    callService('User', 'GetUserPermissionsForEdit', { id }).then((r) => { setPerms(r); setSelected(new Set(r.grantedPermissionNames || [])); }).catch(() => setPerms(false));
  }

  const set = (key, value) => { setUser((u) => ({ ...u, [key]: value })); setDirty(true); };
  const permsDirty = perms && !sameSet(selected, new Set(perms.grantedPermissionNames || []));
  const fromRoles = useMemo(() => (perms ? new Set(perms.rolePermissionNames || []) : null), [perms]);

  async function save(event) {
    event?.preventDefault();
    setBusy(true);
    try {
      if (canDetails && (dirty || !editing)) {
        await callService('User', 'CreateOrUpdateUser', {
          user: { ...user, password: user.password || null }, assignedRoleNames: roles.filter((r) => r.isAssigned).map((r) => r.roleName),
          setRandomPassword: !editing && !user.password, sendActivationEmail: !editing && sendEmail
        });
      }
      if (permsDirty) await callService('User', 'UpdateUserPermissions', { id, grantedPermissionNames: [...selected] });
      notifySuccess(editing ? L('User saved. Changes apply when they next load a page.') : L('User created.'));
      onSaved();
    } catch (_) { setBusy(false); }
  }

  const tabs = [...(canDetails ? [['details', 'Details'], ['roles', 'Roles']] : []), ...(canPermissions ? [['permissions', 'Permissions']] : [])];
  const loading = (canDetails && !user) || (tab === 'permissions' && perms === null);
  const title = editing ? (user ? L('Edit {0}', user.userName) : L('Edit user')) : L('Add user');

  return <Modal title={title} size="xl" onClose={busy ? undefined : onClose} footer={<>
    <div className="hs-actions" style={{ marginRight: 'auto' }}>
      {editing && isGranted('Pages.Administration.Users.Delete') && <button type="button" className="btn s d" disabled={busy} onClick={() => setDialog('delete')}><Icon name="trash" size={15} />{L('Delete')}</button>}
      {editing && canDetails && <button type="button" className="btn s" disabled={busy} onClick={() => setDialog('unlock')}><Icon name="lock" size={15} />{L('Unlock')}</button>}
    </div>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="user-form" className="btn" disabled={busy || loading}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save')}</button>
  </>}>
    {tabs.length > 1 && <Tabs tabs={tabs} value={tab} onChange={setTab} />}
    {loading ? <Skeleton kpis={false} lines={8} /> : <form id="user-form" onSubmit={save}>
      {tab === 'details' && <div className="au-form">
        {USER_FIELDS.map(([key, label, required, type]) => <label key={key}>
          <span>{L(label)}{required && <span className="req"> *</span>}</span>
          <input type={type || 'text'} required={required} value={user[key] ?? ''} autoComplete="off" onChange={(e) => set(key, e.target.value)} />
        </label>)}
        <label><span>{L(editing ? 'New password' : 'Password')}</span>
          <input type="password" autoComplete="new-password" value={user.password} placeholder={L(editing ? 'Leave empty to keep the current one' : 'Leave empty to generate one')} onChange={(e) => set('password', e.target.value)} /></label>
        <label className="ck au-full"><input type="checkbox" checked={!!user.isActive} onChange={(e) => set('isActive', e.target.checked)} />{L('Active: can sign in')}</label>
        <label className="ck au-full"><input type="checkbox" checked={!!user.shouldChangePasswordOnNextLogin} onChange={(e) => set('shouldChangePasswordOnNextLogin', e.target.checked)} />{L('Change password at next sign-in')}</label>
        <label className="ck au-full"><input type="checkbox" checked={!!user.isLockoutEnabled} onChange={(e) => set('isLockoutEnabled', e.target.checked)} />{L('Lock the account after repeated failed sign-ins')}</label>
        {!editing && <label className="ck au-full"><input type="checkbox" checked={sendEmail} onChange={(e) => setSendEmail(e.target.checked)} />{L('Email the user a link to activate the account')}</label>}
      </div>}
      {tab === 'roles' && <>
        <p className="mu" style={{ marginTop: 0 }}>{L('The user gets every permission of each role ticked here.')}</p>
        {ownAccount && <p className="hs-note" style={{ marginTop: 0 }}>{L('This is your own account: only an administrator can change your roles.')}</p>}
        <div className="au-roles">{roles.map((r) => <label key={r.roleId} className={`${r.isAssigned ? 'on' : ''}${ownAccount || r.canChange === false ? ' lock' : ''}`}
          title={r.canChange === false ? L('Only an administrator can give or take this role: it has permissions you do not have.') : undefined}>
          <input type="checkbox" checked={!!r.isAssigned} disabled={ownAccount || r.canChange === false} onChange={(e) => { setRoles((list) => list.map((x) => (x.roleId === r.roleId ? { ...x, isAssigned: e.target.checked } : x))); setDirty(true); }} />
          {r.roleDisplayName || r.roleName}
        </label>)}</div>
      </>}
      {tab === 'permissions' && (perms === false ? <div className="empty"><b>{L('Something went wrong')}</b></div> : <>
        <div className="au-legend">
          <span><span className="pt-tag role">{L('Role')}</span> {L('given by a role')}</span>
          <span><span className="pt-tag add">{L('Added')}</span> {L('given to this user only')}</span>
          <span><span className="pt-tag rem">{L('Removed')}</span> {L('taken from this user only')}</span>
          <span className="sp" />
          <button type="button" className="btn s" disabled={busy} onClick={() => setDialog('reset')}>{L('Use role permissions only')}</button>
        </div>
        {ownAccount && <p className="hs-note" style={{ marginTop: 0 }}>{L('This is your own account. You can change only permissions you have, so a permission you give up can only be given back by an administrator. Changes apply when you next load a page.')}</p>}
        {dirty && <p className="hs-note" style={{ marginTop: 0 }}>{L('Role changes are saved first; the Role marks here still show the roles as they were.')}</p>}
        <PermissionTree permissions={perms.permissions || []} selected={selected} onChange={setSelected} fromRoles={fromRoles} grantable={grantable} />
      </>)}
    </form>}

    {dialog === 'delete' && <ConfirmDialog title={L('Delete {0}?', user?.userName || '')} okText={L('Delete')} okClass="d" busyText={L('Deleting…')} onClose={() => setDialog('')}
      onConfirm={async () => { await callService('User', 'DeleteUser', { id }); notifySuccess(L('User deleted.')); onSaved(); }}>
      <p style={{ margin: 0 }}>{L('They can no longer sign in. Their records stay.')}</p>
    </ConfirmDialog>}
    {dialog === 'unlock' && <ConfirmDialog title={L('Unlock {0}?', user?.userName || '')} okText={L('Unlock')} onClose={() => setDialog('')}
      onConfirm={async () => { await callService('User', 'UnlockUser', { id }); notifySuccess(L('The account is unlocked.')); }}>
      <p style={{ margin: 0 }}>{L('Clears a lockout after failed sign-ins.')}</p>
    </ConfirmDialog>}
    {dialog === 'reset' && <ConfirmDialog title={L('Use role permissions only?')} okText={L('Reset')} okClass="d" busyText={L('Resetting…')} onClose={() => setDialog('')}
      onConfirm={async () => { await callService('User', 'ResetUserSpecificPermissions', { id }); notifySuccess(L('The user now has only the permissions of their roles.')); loadPermissions(); }}>
      <p style={{ margin: 0 }}>{L('Every permission added to or removed from this user alone is cleared.')}</p>
      {ownAccount && <p className="warn" style={{ margin: '8px 0 0', fontSize: 13 }}>{L('This is your own account: you keep only what your roles give, and only an administrator can give the rest back.')}</p>}
    </ConfirmDialog>}
  </Modal>;
}
