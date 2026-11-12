import { useEffect, useMemo, useState } from 'react';
import AdministrationPage from '../administration/index.jsx';
import AccountPage, { useProfilePicture } from '../account/index.jsx';
import GenericCrudPage from '../generic-crud/index.jsx';
import ItemsPage from '../items/index.jsx';
import PurchasesPage from '../purchase-invoices/index.jsx';
import IssuesPage from '../issues/index.jsx';
import InventoryDashboardPage from '../inventory-dashboard/index.jsx';
import StockHistoryPage from '../stock-history/index.jsx';
import ReportsPage from '../reports/index.jsx';
import SetupPage from '../setup/index.jsx';
import HostDashboardPage from '../host-dashboard/index.jsx';
import HostTenantRequestsPage from '../host-tenant-requests/index.jsx';
import HostTenantsPage from '../host-tenants/index.jsx';
import HostEditionsPage from '../host-editions/index.jsx';
import { callService, clearToken, getLanguages, getSession, isAnyGranted, isGranted, L } from '../abp.js';
import { FEATURES, isFeatureEnabled } from '../inventory-shared/features.js';
import Icon from '../inventory-shared/icons.jsx';
import Logo from '../inventory-shared/logo.jsx';
import Modal from '../inventory-shared/modal.jsx';
import { setWorkspaceInfo } from '../inventory-shared/workspace.js';
import './sidebar.css';

// Sidebar, in the mockup's order. `v` matches the mockup's data-v (used by the tour).
const NAV = [
  { v: 'dash', label: 'Dashboard', route: '/dashboard', icon: 'dash', permission: 'Pages.StockPilot' },
  { v: 'items', label: 'Items', route: '/items', icon: 'items', permission: 'Pages.StockPilot.Items' },
  { v: 'purchase', label: 'Purchases', route: '/purchase-invoices', icon: 'purchase', permission: 'Pages.StockPilot.Purchases', feature: FEATURES.purchases },
  { v: 'sale', label: 'Sales & issues', route: '/issues', icon: 'sale', permission: 'Pages.StockPilot.Sales', feature: FEATURES.sales },
  { v: 'move', label: 'Stock history', route: '/stock-history', icon: 'move', permission: 'Pages.StockPilot.StockHistory' },
  { v: 'supplier', label: 'Suppliers', route: '/suppliers', icon: 'supplier', permission: 'Pages.StockPilot.Suppliers' },
  { v: 'reports', label: 'Reports', route: '/reports', icon: 'reports', permission: 'Pages.StockPilot.Reports', feature: FEATURES.reports },
  { v: 'setup', label: 'Setup', route: '/setup', icon: 'setup', permission: 'Pages.StockPilot.Setup' }
];

// The host's sidebar: tenant management instead of the inventory pages.
const HOST_NAV = [
  { v: 'host', label: 'Dashboard', route: '/host', icon: 'dash', permission: 'Pages.Tenants' },
  { v: 'requests', label: 'Store requests', route: '/host/requests', icon: 'file', permission: 'Pages.Tenants.Requests' },
  { v: 'tenants', label: 'Stores', route: '/host/tenants', icon: 'supplier', permission: 'Pages.Tenants' },
  { v: 'editions', label: 'Editions', route: '/host/editions', icon: 'items', permission: 'Pages.Editions' },
  { v: 'users', label: 'Users', route: '/users', icon: 'user', permission: 'Pages.Administration.Users' }
];

/** True for the host (no tenant): host pages and menus instead of a store's. */
export const isHost = () => getSession().tenantId == null;

// Zero's administration pages: not in the mockup, kept under their own heading in the same style.
const ADMIN_NAV = [
  { v: 'roles', label: 'Roles', route: '/roles', icon: 'lock', permission: 'Pages.Administration.Roles' },
  { v: 'users', label: 'Users', route: '/users', icon: 'user', permission: 'Pages.Administration.Users' },
  { v: 'audit', label: 'Audit logs', route: '/audit-logs', icon: 'file', permission: 'Pages.Administration.AuditLogs' },
  { v: 'settings', label: 'Settings', route: '/settings', icon: 'setup', permissions: ['Pages.Administration.Tenant.Settings', 'Pages.Administration.Host.Settings'] },
  { v: 'maintenance', label: 'Maintenance', route: '/maintenance', icon: 'setup', permission: 'Pages.Administration.Host.Maintenance' }
];

// Shown when the user has the permission and the store has the feature.
const allowed = (entry) => isFeatureEnabled(entry.feature) && (entry.permissions ? isAnyGranted(entry.permissions) : isGranted(entry.permission));

// Pages a store feature turns off (the address still works, so it shows a notice instead).
const PAGE_FEATURES = { 'purchase-invoices': FEATURES.purchases, issues: FEATURES.sales, reports: FEATURES.reports };

const ACCENTS = { blue: '#2563eb', red: '#dc2626', orange: '#ea580c', yellow: '#ca8a04', green: '#059669', teal: '#0d9488', purple: '#7c3aed', pink: '#db2777' };
const MODES = { light: 'Day', dark: 'Dark', black: 'Midnight' };
const FALLBACK_LANGUAGES = [{ name: 'en', displayName: 'English' }, { name: 'bn', displayName: 'বাংলা' }, { name: 'hi', displayName: 'हिन्दी' }];

const go = (route) => { window.location.hash = `#${route}`; };

function initials(text) {
  return String(text || '').split(' ').filter(Boolean).slice(0, 2).map((word) => word[0]).join('').toUpperCase() || 'A';
}

/* ---------- sidebar ---------- */

// Open, collapsed to icons, or hidden until the pointer reaches the left edge. Per browser; Ctrl+B toggles.
const SIDEBAR_KEY = 'stockpilot.sidebar';
const SIDEBAR_MODES = { open: 'Open', mini: 'Collapsed', auto: 'Auto-hide' };
const readSidebar = () => {
  try { const value = localStorage.getItem(SIDEBAR_KEY); return SIDEBAR_MODES[value] ? value : 'open'; } catch (_) { return 'open'; }
};

function useSidebarMode() {
  const [mode, setMode] = useState(readSidebar);
  useEffect(() => {
    document.body.classList.toggle('sb-mini', mode === 'mini');
    document.body.classList.toggle('sb-auto', mode === 'auto');
    try { localStorage.setItem(SIDEBAR_KEY, mode); } catch (_) { /* storage may be blocked */ }
  }, [mode]);
  useEffect(() => () => document.body.classList.remove('sb-mini', 'sb-auto'), []);
  useEffect(() => {
    const key = (event) => {
      if ((event.ctrlKey || event.metaKey) && !event.shiftKey && !event.altKey && event.key.toLowerCase() === 'b') {
        event.preventDefault();
        setMode((current) => (current === 'open' ? 'mini' : 'open'));
      }
    };
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  return [mode, setMode];
}

// Menu icon animation (motion.css): data-clk restarts it on every click; cleared when the ripple ends.
const playIcon = (event) => {
  const el = event.currentTarget;
  el.removeAttribute('data-clk');
  void el.offsetWidth;
  el.setAttribute('data-clk', '');
};
const iconDone = (event) => { if (event.animationName === 'mi-ripple') event.currentTarget.removeAttribute('data-clk'); };
const reducedMotion = () => window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
// Lets the icon animation show before the menu closes and the page changes.
const afterIcon = (run) => setTimeout(run, reducedMotion() ? 0 : 220);

function SidebarToggle({ mode, setMode }) {
  const [icon, label, next] = mode === 'open' ? ['chevl', 'Collapse sidebar', 'mini']
    : mode === 'mini' ? ['chevr', 'Expand sidebar', 'open'] : ['pin', 'Keep sidebar open', 'open'];
  return <button type="button" className="sbt mi" data-i={icon} title={`${L(label)} (Ctrl+B)`} aria-label={L(label)}
    onClick={(event) => { playIcon(event); setMode(next); }} onAnimationEnd={iconDone}>
    <Icon name={icon} /><span className="nl">{L(label)}</span>
  </button>;
}

function Sidebar({ path, tenantName, onTour, mode, setMode }) {
  const host = isHost();
  const main = (host ? HOST_NAV : NAV).filter(allowed);
  // The host has Users in its main menu already.
  const admin = ADMIN_NAV.filter((entry) => allowed(entry) && !(host && entry.v === 'users'));
  const pending = usePendingRequests(host);
  const link = (entry) => <a key={entry.v} data-v={entry.v} data-i={entry.icon} className={path === entry.route ? 'mi on' : 'mi'} href={`#${entry.route}`}
    title={mode === 'open' ? undefined : L(entry.label)} onClick={playIcon} onAnimationEnd={iconDone}>
    <Icon name={entry.icon} /><span className="nl">{L(entry.label)}</span>
    {entry.v === 'requests' && pending > 0 && <span className="nvb" title={L('{0} waiting for review', pending)}>{pending}</span>}
  </a>;
  return <aside id="nav" aria-label={L('Main menu')}>
    <div className="brand"><Logo /><span className="nl">StockPilot</span></div>
    {host && main.length > 0 && <div className="nvs">{L('Host')}</div>}
    {main.map(link)}
    {admin.length > 0 && <><div className="nvs">{L('Administration')}</div>{admin.map(link)}</>}
    {!host && <a className="tourl mi" data-i="compass" onClick={(event) => { playIcon(event); onTour(); }} onAnimationEnd={iconDone} title={mode === 'open' ? undefined : L('Take a tour')}>
      <Icon name="compass" /><span className="nl">{L('Take a tour')}</span>
    </a>}
    <SidebarToggle mode={mode} setMode={setMode} />
    {host
      ? <div className="ft">{L('Host')}<br /><b style={{ color: 'var(--tx)' }}>{L('All stores')}</b><br />{L('You manage every store from here.')}</div>
      : <div className="ft">{L('Tenant')}<br /><b style={{ color: 'var(--tx)' }}>{tenantName || L('Host')}</b><br />{L('All data is tenant-isolated and fully audited.')}</div>}
  </aside>;
}

/** The number of store requests waiting for review (host only); refreshed when the requests page changes one. */
function usePendingRequests(host) {
  const [count, setCount] = useState(0);
  useEffect(() => {
    if (!host || !isGranted('Pages.Tenants.Requests')) return undefined;
    const load = () => callService('TenantRequest', 'GetCounts', {}).then((result) => setCount(result.pending || 0)).catch(() => { /* badge only */ });
    load();
    window.addEventListener('stockpilot:requests-changed', load);
    return () => window.removeEventListener('stockpilot:requests-changed', load);
  }, [host]);
  return count;
}

/* ---------- top bar ---------- */

function useOutsideClose(close) {
  useEffect(() => {
    const onClick = () => close();
    const onKey = (event) => { if (event.key === 'Escape') close(); };
    document.addEventListener('click', onClick);
    window.addEventListener('keydown', onKey);
    return () => { document.removeEventListener('click', onClick); window.removeEventListener('keydown', onKey); };
  }, []);
}

const NEW_ACTIONS = [
  { icon: 'purchase', label: 'Receive stock', route: '/purchase-invoices?new=1', permission: 'Pages.StockPilot.Purchases.Create', feature: FEATURES.purchases },
  { icon: 'sale', label: 'Sell or issue stock', route: '/issues?new=1', permission: 'Pages.StockPilot.Sales.Create', feature: FEATURES.sales },
  { icon: 'items', label: 'Add an item', route: '/items?new=1', permission: 'Pages.StockPilot.Items.Create' },
  { icon: 'supplier', label: 'Add a supplier', route: '/suppliers?new=1', permission: 'Pages.StockPilot.Suppliers.Create' }
];
const HOST_NEW_ACTIONS = [
  { icon: 'supplier', label: 'Add store', route: '/host/tenants?new=1', permission: 'Pages.Tenants.Create' }
];
const newActions = () => (isHost() ? HOST_NEW_ACTIONS : NEW_ACTIONS).filter(allowed);

function TopBar({ user, tenantName, preferences, onTour, onPalette, sidebar, setSidebar }) {
  const [menu, setMenu] = useState('');
  const [signOut, setSignOut] = useState(false);
  useOutsideClose(() => setMenu(''));
  const toggle = (name) => (event) => { event.stopPropagation(); setMenu(menu === name ? '' : name); };
  const keep = (event) => event.stopPropagation();

  const fullName = [user.name, user.surname].filter(Boolean).join(' ') || user.userName || '';
  const picture = useProfilePicture();
  const languages = getLanguages().length ? getLanguages() : FALLBACK_LANGUAGES;
  const language = languages.find((entry) => entry.name === preferences.language) || languages[0];
  const actions = newActions();
  const menuItem = (icon, label, onClick, className) => <a className={className} onClick={() => { setMenu(''); onClick(); }}><Icon name={icon} size={16} />{L(label)}</a>;

  return <div className="top" id="top">
    <div className="srch" onClick={onPalette}><Icon name="search" size={16} />{L('Search items, pages, actions…')}<kbd>Ctrl K</kbd></div>
    <div className="sp" />
    {actions.length > 0 && <div style={{ position: 'relative' }}>
      <button className="btn mi-new" aria-expanded={menu === 'new'} onClick={toggle('new')}><Icon name="plus" size={16} />{L('New')}</button>
      {menu === 'new' && <div className="mn">{actions.map((action) => <a key={action.label} className="mi" data-i={action.icon} onAnimationEnd={iconDone}
        onClick={(event) => { event.stopPropagation(); playIcon(event); afterIcon(() => { setMenu(''); go(action.route); }); }}>
        <Icon name={action.icon} size={17} />{L(action.label)}
      </a>)}</div>}
    </div>}
    <div className="tl" style={{ display: 'flex', gap: 8 }}><div style={{ position: 'relative' }}>
      <span className="chip" onClick={toggle('language')}><Icon name="globe" size={15} />{language.displayName || language.name}</span>
      {menu === 'language' && <div className="mn" style={{ minWidth: 150 }}>{languages.map((entry) => <a key={entry.name} onClick={() => { setMenu(''); preferences.setLanguage(entry.name); }}>{entry.displayName || entry.name}{entry.name === language.name ? ' ✓' : ''}</a>)}</div>}
    </div></div>
    <div style={{ position: 'relative' }}>
      <span className="chip" id="thc" onClick={toggle('theme')}><Icon name="palette" size={15} />{L('Theme')}</span>
      {menu === 'theme' && <div className="mn" style={{ minWidth: 300, padding: 10 }} onClick={keep}>
        <div className="mu" style={{ margin: '0 4px 6px' }}>{L('Mode')}</div>
        <div className="tm">{Object.entries(MODES).map(([mode, label]) => <button key={mode} className={preferences.mode === mode ? 'on' : ''} onClick={() => preferences.setMode(mode)}><Icon name={mode === 'light' ? 'sun' : 'moon'} size={14} />{L(label)}</button>)}</div>
        <div className="mu" style={{ margin: '12px 4px 6px' }}>{L('Sidebar')}</div>
        <div className="tm">{Object.entries(SIDEBAR_MODES).map(([mode, label]) => <button key={mode} className={sidebar === mode ? 'on' : ''} onClick={() => setSidebar(mode)}><Icon name={mode === 'open' ? 'sidebar' : mode === 'mini' ? 'chevl' : 'pin'} size={14} />{L(label)}</button>)}</div>
        <div className="mu" style={{ margin: '12px 4px 6px' }}>{L('Accent colour')}</div>
        <div className="sw">{Object.entries(ACCENTS).map(([accent, colour]) => <button key={accent} title={accent} aria-label={accent} className={preferences.accent === accent ? 'on' : ''} style={{ background: colour }} onClick={() => preferences.setAccent(accent)} />)}</div>
      </div>}
    </div>
    <div style={{ position: 'relative' }}>
      <div className={`me${picture ? ' has-pic' : ''}`} title={L('Profile')} onClick={toggle('profile')}>{picture ? <img src={picture} alt="" /> : initials(fullName)}</div>
      {menu === 'profile' && <div className="mn" style={{ minWidth: 260, padding: 0 }} onClick={keep}>
        <div className="pmh"><div className={`me${picture ? ' has-pic' : ''}`}>{picture ? <img src={picture} alt="" /> : initials(fullName)}</div><div style={{ minWidth: 0 }}><b>{fullName}</b><div className="mu">{user.emailAddress}</div><div className="mu">{user.userName}{tenantName ? ` · ${tenantName}` : ''}</div></div></div>
        <div style={{ padding: 6 }}>
          {menuItem('user', 'Edit profile', () => go('/account?section=my-settings'))}
          {menuItem('camera', 'Change profile picture', () => go('/account?section=profile-picture'))}
          {menuItem('lock', 'Change password', () => go('/account?section=change-password'))}
          {menuItem('file', 'Login attempts', () => go('/account?section=login-attempts'))}
          {!isHost() && menuItem('compass', 'Take a tour', onTour)}
        </div>
        <div style={{ padding: 6, borderTop: '1px solid var(--bd)' }}>{menuItem('logout', 'Sign out', () => setSignOut(true), 'out')}</div>
      </div>}
    </div>
    {signOut && <SignOutDialog onClose={() => setSignOut(false)} />}
  </div>;
}

function SignOutDialog({ onClose }) {
  const [busy, setBusy] = useState(false);
  function signOut() {
    setBusy(true);
    clearToken();
    window.location.hash = '#/';
    window.location.reload();
  }
  return <Modal title={L('Sign out?')} onClose={onClose} footer={<><button className="btn s" onClick={onClose}>{L('Cancel')}</button><button className="btn d" disabled={busy} onClick={signOut}>{busy ? <><i className="spn" /> {L('Signing out…')}</> : L('Sign out')}</button></>}>
    <p style={{ margin: 0 }}>{L('You will need to sign in again.')}</p>
  </Modal>;
}

/* ---------- command palette (Ctrl/Cmd+K) ---------- */

function Palette({ onClose, onTour }) {
  const [query, setQuery] = useState('');
  const [items, setItems] = useState([]);
  const [focus, setFocus] = useState(0);
  const canSearchItems = isGranted('Pages.StockPilot.Items');

  // Items come from the server (ItemMaster.SearchForDocument), 300ms after typing stops.
  useEffect(() => {
    const text = query.trim();
    if (!text || !canSearchItems) { setItems([]); return undefined; }
    let live = true;
    const timer = setTimeout(() => {
      callService('ItemMaster', 'SearchForDocument', { text, maxResultCount: 6 })
        .then((result) => { if (live) setItems(result.items || []); })
        .catch(() => { /* abp.ajax already showed the error */ });
    }, 300);
    return () => { live = false; clearTimeout(timer); };
  }, [query]);

  const entries = useMemo(() => {
    const text = query.trim().toLowerCase();
    const fixed = [
      ...newActions().map((action) => ({ title: L(action.label), type: L('Action'), run: () => go(action.route) })),
      ...(isHost() ? [] : [{ title: L('Take a tour'), type: L('Help'), run: onTour }]),
      ...(isHost() ? HOST_NAV : NAV).filter(allowed).map((entry) => ({ title: `${L('Go to')} ${L(entry.label)}`, type: L('Page'), run: () => go(entry.route) }))
    ].filter((entry) => !text || `${entry.title} ${entry.type}`.toLowerCase().includes(text));
    const found = items.map((item) => ({ title: item.itemName, type: `${L('Item')} · ${item.itemCode}`, run: () => go(`/items?open=${item.id}`) }));
    return [...fixed, ...found].slice(0, 8);
  }, [query, items]);

  useEffect(() => setFocus(0), [entries.length]);
  const run = (entry) => { onClose(); entry.run(); };

  return <Modal title={L('Search or jump to')} onClose={onClose}>
    <input style={{ width: '100%' }} autoFocus value={query} placeholder={L('Type an item, page or action…')}
      onChange={(event) => setQuery(event.target.value)}
      onKeyDown={(event) => {
        if (event.key === 'ArrowDown') { event.preventDefault(); setFocus(Math.min(focus + 1, entries.length - 1)); }
        if (event.key === 'ArrowUp') { event.preventDefault(); setFocus(Math.max(focus - 1, 0)); }
        if (event.key === 'Enter' && entries[focus]) run(entries[focus]);
      }} />
    <div style={{ marginTop: 10, maxHeight: 320, overflow: 'auto' }}>
      {entries.map((entry, index) => <div key={`${entry.title}-${index}`} className={`sg ${index === focus ? 'on' : ''}`} onMouseEnter={() => setFocus(index)} onClick={() => run(entry)}><b>{entry.title}</b><span className="mu">{entry.type}</span></div>)}
      {!entries.length && <div className="empty">{L('No matches')}</div>}
    </div>
  </Modal>;
}

/* ---------- shell ---------- */

function FeatureOff() {
  return <div className="card" style={{ marginTop: 8 }}><div className="empty">
    <b>{L('This feature is not enabled for your store')}</b>{L('Ask us if you need it.')}
    <div style={{ marginTop: 14 }}><button className="btn s" onClick={() => go('/dashboard')}>{L('Go to Dashboard')}</button></div>
  </div></div>;
}

export default function App({ path, page, genericCrudPage, permissions = [], loginInfo, preferences }) {
  const [palette, setPalette] = useState(false);
  const [sidebar, setSidebar] = useSidebarMode();
  const user = loginInfo?.user || {};
  const tenantName = loginInfo?.tenant?.name || loginInfo?.tenant?.tenancyName || '';
  setWorkspaceInfo({ tenantName, userName: [user.name, user.surname].filter(Boolean).join(' ') || user.userName || '' });
  const startTour = () => { setPalette(false); preferences.setTourStep(0); preferences.setTourRunning(true); };

  useEffect(() => {
    document.body.classList.add('sp-shell');
    return () => document.body.classList.remove('sp-shell');
  }, []);
  useEffect(() => {
    const key = (event) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); setPalette(true); } };
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  useEffect(() => { window.scrollTo(0, 0); }, [path]);

  let content;
  if (PAGE_FEATURES[page] && !isFeatureEnabled(PAGE_FEATURES[page])) content = <FeatureOff />;
  else if (genericCrudPage) content = <GenericCrudPage config={genericCrudPage} />;
  else if (page === 'items') content = <ItemsPage />;
  else if (page === 'purchase-invoices') content = <PurchasesPage />;
  else if (page === 'issues') content = <IssuesPage />;
  else if (page === 'dashboard') content = <InventoryDashboardPage user={user} />;
  else if (page === 'stock-history') content = <StockHistoryPage />;
  else if (page === 'reports') content = <ReportsPage />;
  else if (page === 'setup') content = <SetupPage />;
  else if (page === 'host-dashboard') content = <HostDashboardPage user={user} />;
  else if (page === 'host-requests') content = <HostTenantRequestsPage />;
  else if (page === 'host-tenants') content = <HostTenantsPage />;
  else if (page === 'host-editions') content = <HostEditionsPage />;
  else if (page === 'account') content = <AccountPage user={user} tenantName={tenantName} />;
  else content = <AdministrationPage page={page} permissions={permissions} />;

  return <div className="sp-app">
    <Sidebar path={path} tenantName={tenantName} onTour={startTour} mode={sidebar} setMode={setSidebar} />
    <main>
      <TopBar user={user} tenantName={tenantName} preferences={preferences} onTour={startTour} onPalette={() => setPalette(true)} sidebar={sidebar} setSidebar={setSidebar} />
      <div className="pg" id="pg" key={path}>{content}</div>
    </main>
    {palette && <Palette onClose={() => setPalette(false)} onTour={startTour} />}
  </div>;
}
