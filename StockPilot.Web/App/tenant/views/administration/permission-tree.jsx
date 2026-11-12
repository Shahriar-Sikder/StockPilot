import { useMemo, useState } from 'react';
import { L } from '../abp.js';
import './permissions.css';

// Leaf permissions shown as action chips next to their page (by the last part of the name).
const ACTIONS = {
  Create: 'Create', Edit: 'Edit', Delete: 'Delete', Post: 'Post to stock', Approve: 'Approve', Reject: 'Reject', Cancel: 'Cancel & reverse',
  ChangePermissions: 'Change permissions', ChangeFeatures: 'Change features', Impersonation: 'Sign in as', ChangeTexts: 'Change texts',
  ManageOrganizationTree: 'Manage tree', ManageMembers: 'Manage members', Dashboard: 'Dashboard'
};
const ACTION_ORDER = ['Create', 'Edit', 'Delete', 'Post', 'Approve', 'Reject', 'Cancel'];
const actionKey = (name) => name.slice(name.lastIndexOf('.') + 1);

/** The flat permission list (parentName links, definition order) as a tree: { node, children, actions } per page. */
function buildTree(permissions) {
  const byName = new Map(permissions.map((p) => [p.name, { permission: p, children: [], actions: [] }]));
  const roots = [];
  for (const p of permissions) {
    const item = byName.get(p.name);
    const parent = p.parentName && byName.get(p.parentName);
    if (parent) parent.children.push(item); else roots.push(item);
  }
  // A child with no children of its own and a known action name is an action chip of its parent.
  for (const item of byName.values()) {
    const leaves = item.children.filter((c) => !c.children.length && ACTIONS[actionKey(c.permission.name)]);
    if (leaves.length) {
      item.actions = leaves.sort((a, b) => order(a) - order(b));
      item.children = item.children.filter((c) => !leaves.includes(c));
    }
  }
  return roots;
}
const order = (item) => { const i = ACTION_ORDER.indexOf(actionKey(item.permission.name)); return i < 0 ? 99 : i; };

const descendants = (item) => [item.permission.name, ...item.actions.map((a) => a.permission.name), ...item.children.flatMap(descendants)];

/**
 * The permission tree of the Roles and Users editors. Each page or list is a row: its own box is "view" (open the page),
 * its actions (Create, Edit, Delete, Approve, …) are chips next to it, so any combination can be given.
 *   - giving an action also gives its page and the pages above it (an action without the page is of no use)
 *   - taking a page away takes its actions and sub-pages away
 *   - a group header (a page with sub-pages) gives or takes its whole branch
 * fromRoles (users only): the names the user's roles give; rows then show "From role" / "Added" / "Removed".
 * grantable: the names the signed-in user may give or take (they hold them); the rest are greyed out. null = all.
 */
export default function PermissionTree({ permissions, selected, onChange, fromRoles, grantable = null, readOnly = false }) {
  const tree = useMemo(() => buildTree(permissions), [permissions]);
  const parents = useMemo(() => new Map(permissions.map((p) => [p.name, p.parentName])), [permissions]);
  const [query, setQuery] = useState('');
  const [collapsed, setCollapsed] = useState(() => new Set());
  const text = query.trim().toLowerCase();
  const canChange = (name) => !readOnly && (!grantable || grantable.has(name));

  // Only names the user may change are touched; a parent page is added only when it can be.
  const set = (names, on) => {
    const next = new Set(selected);
    for (const name of names.filter(canChange)) {
      if (on) { next.add(name); let p = parents.get(name); while (p) { if (canChange(p)) next.add(p); p = parents.get(p); } } else next.delete(name);
    }
    onChange(next);
  };
  const lockedTitle = L('You do not have this permission, so you cannot give or take it away.');

  const matches = (item) => !text || `${item.permission.displayName} ${item.permission.name}`.toLowerCase().includes(text)
    || item.actions.some((a) => L(ACTIONS[actionKey(a.permission.name)]).toLowerCase().includes(text))
    || item.children.some(matches);

  const total = permissions.length;
  const granted = permissions.filter((p) => selected.has(p.name)).length;
  const toggleCollapse = (name) => setCollapsed((c) => { const n = new Set(c); if (n.has(name)) n.delete(name); else n.add(name); return n; });
  const groups = (items) => items.flatMap((i) => (i.children.length ? [i.permission.name, ...groups(i.children)] : []));

  const origin = (name) => {
    if (!fromRoles) return null;
    const role = fromRoles.has(name), has = selected.has(name);
    if (role && has) return <span className="pt-tag role" title={L('Given by the user’s roles')}>{L('Role')}</span>;
    if (!role && has) return <span className="pt-tag add" title={L('Given to this user only')}>{L('Added')}</span>;
    if (role && !has) return <span className="pt-tag rem" title={L('Taken away from this user only')}>{L('Removed')}</span>;
    return null;
  };

  const chip = (action) => {
    const name = action.permission.name;
    const on = selected.has(name);
    const changed = fromRoles && fromRoles.has(name) !== on;
    return <label key={name} className={`pt-chip${on ? ' on' : ''}${changed ? ' chg' : ''}${canChange(name) ? '' : ' lock'}`} title={canChange(name) || readOnly ? action.permission.description || action.permission.displayName : lockedTitle}>
      <input type="checkbox" checked={on} disabled={!canChange(name)} onChange={(e) => set([name], e.target.checked)} />
      {L(ACTIONS[actionKey(name)])}
    </label>;
  };

  const row = (item, depth) => {
    if (!matches(item)) return null;
    const p = item.permission;
    const isGroup = item.children.length > 0;
    const all = descendants(item);
    const count = all.filter((n) => selected.has(n)).length;
    const open = text || !collapsed.has(p.name);
    const on = selected.has(p.name);
    return <div key={p.name} className={`pt-node${isGroup ? ' grp' : ''}`} style={{ '--d': depth }}>
      <div className="pt-row">
        {isGroup ? <button type="button" className="pt-caret" aria-expanded={!!open} aria-label={open ? L('Collapse') : L('Expand')} onClick={() => toggleCollapse(p.name)}>{open ? '▾' : '▸'}</button> : <span className="pt-caret" />}
        <label className={`pt-name${canChange(p.name) || isGroup ? '' : ' lock'}`} title={canChange(p.name) || readOnly ? p.description || p.name : lockedTitle}>
          <input type="checkbox" checked={on} disabled={readOnly || (isGroup ? !all.some(canChange) : !canChange(p.name))}
            ref={(el) => { if (el) el.indeterminate = isGroup && !on && count > 0; }}
            onChange={(e) => (isGroup ? set(all, e.target.checked) : set(e.target.checked ? [p.name] : all, e.target.checked))} />
          <span>{p.displayName}</span>
          {!isGroup && item.actions.length > 0 && <small className="pt-view">{L('View')}</small>}
        </label>
        {origin(p.name)}
        {isGroup && <span className="pt-count">{count}/{all.length}</span>}
        {item.actions.length > 0 && <div className="pt-actions">{item.actions.map(chip)}</div>}
      </div>
      {isGroup && open && <div className="pt-kids">{item.children.map((c) => row(c, depth + 1))}</div>}
    </div>;
  };

  return <div className="pt">
    <div className="pt-bar">
      <input className="sr" placeholder={L('Find a permission…')} value={query} onChange={(e) => setQuery(e.target.value)} aria-label={L('Find a permission…')} />
      <span className="mu">{L('{0} of {1} given', granted, total)}</span>
      <span className="sp" />
      <button type="button" className="btn s" onClick={() => setCollapsed(new Set())}>{L('Expand all')}</button>
      <button type="button" className="btn s" onClick={() => setCollapsed(new Set(groups(tree)))}>{L('Collapse all')}</button>
      {!readOnly && <button type="button" className="btn s" onClick={() => set(permissions.map((p) => p.name), true)}>{L('Select all')}</button>}
      {!readOnly && <button type="button" className="btn s" onClick={() => set(permissions.map((p) => p.name), false)}>{L('Clear all')}</button>}
    </div>
    <p className="mu pt-help">{L('Tick a page to let them open it. Tick the actions they may do there. Giving an action also gives the page; taking a page away takes its actions too.')}{grantable && !readOnly && <> {L('Greyed-out permissions are ones you do not have: only an administrator can change them.')}</>}</p>
    {/* ABP's root "Pages" only holds everything else: show its children as the top level (it is given with any of them). */}
    <div className="pt-tree">{tree.flatMap((item) => (item.permission.name === 'Pages' ? item.children : [item])).map((item) => row(item, 0))}</div>
  </div>;
}
