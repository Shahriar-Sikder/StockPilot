import React, { createContext, useContext, useEffect, useLayoutEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import App, { isHost } from './index.jsx';
import '../layout/index.css';
import PublicLayout from '../layout/index.jsx';
import LandingPage from '../landing/index.jsx';
import HomePage from '../home/index.jsx';
import LoginPage from '../login/index.jsx';
import HostLoginPage from '../host-login/index.jsx';
import TenantRegistrationPage from '../tenant-registration/index.jsx';
import RegisterStatusPage from '../register-status/index.jsx';
import ForgotPasswordPage from '../forgot-password/index.jsx';
import AboutPage from '../about/index.jsx';
import ErrorPage from '../error/index.jsx';
import './administration.css';
import { HtmlView } from '../template.jsx';
import loadingTemplate from './main.html?raw';
import ToastHost from '../toast.jsx';
import { notify } from '../toast.js';
import { getGenericCrudPage } from '../generic-crud/pages.js';
import '../toast.css';
import WelcomeDialog from '../welcome/index.jsx';
import { callService, currentLanguage, getSession, grantedPermissionNames, initAbp, installUi, isSignedIn, L, setLanguage } from '../abp.js';
import ConfirmHost, { openAlert, openConfirm } from '../inventory-shared/confirm.jsx';
import IfCost from '../inventory-shared/if-cost.jsx';
import { FEATURES, isFeatureEnabled } from '../inventory-shared/features.js';
import { installButtonMotion } from '../inventory-shared/motion.js';
import ProgressBar from '../inventory-shared/progress-bar.jsx';
// The mockup's design system, loaded last so it wins over older page styles.
import './theme.css';
import '../inventory-shared/stockpilot.css';
import '../inventory-shared/motion.css';

export const PreferenceContext = createContext(null);

const read = (key, fallback) => { try { return localStorage.getItem(key) || fallback; } catch (_) { return fallback; } };
const write = (key, value) => { try { localStorage.setItem(key, value); } catch (_) { /* storage may be blocked */ } };

/** Theme mode and accent are per-browser (localStorage). Language is the ABP culture (cookie + reload). */
function PreferenceRoot({ children }) {
  const [mode, setMode] = useState(read('stockpilot.theme.mode', window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'));
  const [accent, setAccent] = useState(read('stockpilot.theme.accent', 'blue'));
  const [tourRunning, setTourRunning] = useState(false);
  const [tourStep, setTourStep] = useState(0);
  useLayoutEffect(() => {
    document.documentElement.dataset.theme = mode;
    document.documentElement.dataset.accent = accent;
    write('stockpilot.theme.mode', mode);
    write('stockpilot.theme.accent', accent);
  }, [mode, accent]);
  const language = currentLanguage();
  useEffect(() => { document.documentElement.lang = language; }, [language]);
  const value = { mode, setMode, accent, setAccent, language, setLanguage, tourRunning, setTourRunning, tourStep, setTourStep };
  return <PreferenceContext.Provider value={value}>{children}<TourOverlay preferences={value} /></PreferenceContext.Provider>;
}

/* ---------- tour (the mockup's TOUR, selectors on the new markup) ---------- */

const TOUR_ROUTES = { dash: '/dashboard', items: '/items', purchase: '/purchase-invoices', sale: '/issues', reports: '/reports' };
// Tour steps on a page the store's features turn off are skipped.
const TOUR_FEATURES = { purchase: FEATURES.purchases, sale: FEATURES.sales, reports: FEATURES.reports };
const TOUR = [
  ['', 'dash', 'Welcome to StockPilot', 'This one-minute tour shows how to receive stock, sell it and read your reports.'],
  ['#nav', 'dash', 'Menu', 'Move between Dashboard, Items, Purchases, Sales, Stock history, Suppliers, Reports and Setup.'],
  ['#nav a[data-v=setup]', 'dash', 'Start with Setup', 'First add categories, units, currencies and suppliers. Pictures are optional.'],
  ['#top .btn', 'dash', 'New button', 'Create anything from here: receive stock, sell stock, add an item or a supplier.'],
  // The fifth entry is the text used while "Hide cost and values" is on (no stock value).
  ['.kp', 'dash', 'Your numbers', 'Stock value, stock in and out, and what needs attention, updated live.', 'Items in stock, what came in and went out, and what needs attention, updated live.'],
  ['#pg .hd', 'items', 'Items', 'Click an item to see its stock and history, then receive or issue it.'],
  ['#pg .gt', 'items', 'Search, sort, export', 'Every table has search, sortable headers, scrolling, paging and Export Excel.'],
  ['#pg .hd .ac', 'purchase', 'Receive stock', 'Create a purchase invoice, attach documents, or import many invoices from Excel.'],
  ['#pg .hd .ac', 'sale', 'Sell or issue', 'Pick items, watch the stock warning, then post to reduce stock.'],
  ['.rpc', 'reports', 'Reports', 'Run, print or export five reports.'],
  ['#top .tl', 'dash', 'Language', 'Switch the interface language here.'],
  ['#thc', 'dash', 'Theme', 'Change the colours here: Day, Dark or Midnight, plus eight accent colours.'],
  ['#top .me', 'dash', 'Your profile', 'Edit your profile, change your picture or password, and sign out from here.']
];

function TourOverlay({ preferences }) {
  const steps = useMemo(() => TOUR.filter((entry) => isFeatureEnabled(TOUR_FEATURES[entry[1]])), []);
  const { tourRunning, tourStep, setTourRunning, setTourStep } = preferences;
  const [box, setBox] = useState(null);
  const [, redraw] = useState(0);
  const step = steps[Math.min(tourStep, steps.length - 1)];

  function end(finished) {
    setTourRunning(false);
    setTourStep(0);
    window.location.hash = '#/dashboard';
    notify('info', L(finished ? 'Tour finished. Restart it anytime from Take a tour.' : 'Tour closed. Restart it anytime from Take a tour.'));
  }
  const goTo = (index) => (index >= steps.length ? end(true) : setTourStep(index));

  useEffect(() => {
    if (!tourRunning) return undefined;
    const route = TOUR_ROUTES[step[1]];
    if ((window.location.hash.slice(1).split('?')[0] || '/dashboard') !== route) window.location.hash = `#${route}`;
    // Wait for the page to render (and its data skeleton to settle) before measuring the target.
    const timer = setTimeout(() => {
      const node = step[0] ? document.querySelector(step[0]) : null;
      if (node && node.scrollIntoView) node.scrollIntoView({ block: 'center' });
      requestAnimationFrame(() => setBox(node ? node.getBoundingClientRect() : null));
    }, 350);
    return () => clearTimeout(timer);
  }, [tourRunning, tourStep]);

  useEffect(() => {
    if (!tourRunning) return undefined;
    const key = (event) => {
      if (event.key === 'ArrowRight') goTo(tourStep + 1);
      if (event.key === 'ArrowLeft' && tourStep > 0) goTo(tourStep - 1);
      if (event.key === 'Escape') end(false);
    };
    // Like the mockup's resize handler: measure the highlighted element again.
    const resize = () => {
      const node = step[0] ? document.querySelector(step[0]) : null;
      setBox(node ? node.getBoundingClientRect() : null);
      redraw((n) => n + 1);
    };
    window.addEventListener('keydown', key);
    window.addEventListener('resize', resize);
    return () => { window.removeEventListener('keydown', key); window.removeEventListener('resize', resize); };
  }, [tourRunning, tourStep]);

  if (!tourRunning) return null;
  const width = window.innerWidth, height = window.innerHeight, panelWidth = Math.min(340, width - 24);
  let x = (width - panelWidth) / 2, y = height / 2 - 100;
  if (box) { x = Math.min(Math.max(12, box.left), width - panelWidth - 12); y = box.bottom + 14; if (y + 190 > height) y = Math.max(12, box.top - 200); }
  const last = tourStep === steps.length - 1;
  return <div id="tr">
    {box ? <div className="hl" style={{ left: box.left - 6, top: box.top - 6, width: box.width + 12, height: box.height + 12 }} /> : <div className="ov2" />}
    <div className="tp" style={{ left: x, top: y, width: panelWidth }} key={tourStep}>
      <div className="mu">{tourStep + 1} / {steps.length}</div>
      <h3>{L(step[2])}</h3>
      <p><IfCost when={!!step[4]} fallback={step[4] && L(step[4])}>{L(step[3])}</IfCost></p>
      <div className="ac" style={{ justifyContent: 'space-between' }}>
        <button className="lnk mu" onClick={() => end(false)}>{L('Skip tour')}</button>
        <div className="ac">{tourStep > 0 && <button className="btn s" onClick={() => goTo(tourStep - 1)}>{L('Back')}</button>}<button className="btn" onClick={() => goTo(tourStep + 1)}>{L(last ? 'Finish' : 'Next')}</button></div>
      </div>
    </div>
  </div>;
}

/* ---------- routing ---------- */

const pages = {
  '/': LandingPage,
  '/landing': LandingPage,
  '/home': HomePage,
  '/login': LoginPage,
  '/host-login': HostLoginPage,
  '/register': TenantRegistrationPage,
  '/register-status': RegisterStatusPage,
  '/tenant-registration': TenantRegistrationPage,
  '/forgot-password': ForgotPasswordPage,
  '/about': AboutPage,
  '/error': ErrorPage
};

// Routes rendered inside the signed-in shell (value = page key).
const shellRoutes = {
  '/dashboard': 'dashboard', '/purchase-invoices': 'purchase-invoices', '/issues': 'issues', '/items': 'items',
  '/stock-history': 'stock-history', '/reports': 'reports', '/setup': 'setup',
  '/host': 'host-dashboard', '/host/requests': 'host-requests', '/host/tenants': 'host-tenants', '/host/editions': 'host-editions',
  '/roles': 'roles', '/users': 'users', '/audit-logs': 'audit', '/settings': 'settings', '/maintenance': 'maintenance', '/account': 'account'
};
// Shared by the host and the stores; every other shell route belongs to one side only.
const sharedRoutes = new Set(['/roles', '/users', '/audit-logs', '/settings', '/maintenance', '/account']);
const isHostRoute = (path) => path === '/host' || path.startsWith('/host/');
const homeRoute = () => (isHost() ? '/host' : '/dashboard');
const publicRoutes = new Set(['/', '/landing', '/login', '/host-login', '/register', '/register-status', '/tenant-registration']);

const welcomeKey = (session) => `stockpilot.first-signin.${session.tenantId || 'host'}.${session.userId}`;

function AppRouter() {
  const preferences = useContext(PreferenceContext);
  const [route, setRoute] = useState(window.location.hash.slice(1) || '/');
  const [loginInfo, setLoginInfo] = useState(null);
  const [showWelcome, setShowWelcome] = useState(false);
  const signedIn = isSignedIn();

  useEffect(() => {
    const onRouteChange = () => setRoute(window.location.hash.slice(1) || '/');
    // A redirect made during the first render (host -> #/host) can fire hashchange before this listener exists.
    onRouteChange();
    window.addEventListener('hashchange', onRouteChange);
    return () => window.removeEventListener('hashchange', onRouteChange);
  }, []);

  useEffect(() => {
    if (!signedIn) return undefined;
    // Loaded again after the profile page saves (name, picture), so the top bar shows the change.
    const load = () => callService('Session', 'GetCurrentLoginInformations').then(setLoginInfo).catch(() => setLoginInfo((current) => current || {}));
    load();
    window.addEventListener('stockpilot:profile-changed', load);
    setShowWelcome(read(welcomeKey(getSession()), '') !== 'complete');
    return () => window.removeEventListener('stockpilot:profile-changed', load);
  }, [signedIn]);

  const path = route.split('?')[0];
  if (!signedIn && !publicRoutes.has(path) && !pages[path]) { window.location.hash = '#/'; return null; }
  if (signedIn && publicRoutes.has(path)) { window.location.hash = `#${homeRoute()}`; return null; }

  const genericCrudPage = getGenericCrudPage(path);
  // The host sees only host pages, a store only store pages (plus the shared administration pages).
  if (signedIn && (shellRoutes[path] || genericCrudPage) && !sharedRoutes.has(path) && isHostRoute(path) !== isHost()) { window.location.hash = `#${homeRoute()}`; return null; }
  if (signedIn && (shellRoutes[path] || genericCrudPage)) {
    if (!loginInfo) return <HtmlView template={loadingTemplate} />;
    const finishWelcome = (startTour) => {
      write(welcomeKey(getSession()), 'complete');
      setShowWelcome(false);
      if (startTour) setTimeout(() => { preferences.setTourStep(0); preferences.setTourRunning(true); }, 150);
    };
    return <>
      <App path={path} page={shellRoutes[path]} genericCrudPage={genericCrudPage} permissions={grantedPermissionNames()} loginInfo={loginInfo} preferences={preferences} />
      {showWelcome && path === '/dashboard' && !isHost() && <WelcomeDialog onClose={() => finishWelcome(false)} onStart={() => finishWelcome(true)} />}
    </>;
  }

  const Page = pages[path] || ErrorPage;
  return <PublicLayout authenticated={signedIn}><Page /></PublicLayout>;
}

/* ---------- bootstrap ---------- */

function BootError({ error }) {
  return <div className="route-loading" role="alert">{error.message || 'The application could not start.'}</div>;
}

const root = createRoot(document.getElementById('root'));
root.render(<HtmlView template={loadingTemplate} />);

initAbp()
  .then(() => {
    installButtonMotion();
    installUi({
      toast: (type, message) => notify(type === 'warn' ? 'warning' : type, message),
      alert: openAlert,
      confirmDialog: openConfirm
    });
    root.render(<React.StrictMode><PreferenceRoot><AppRouter /><ToastHost /><ConfirmHost /><ProgressBar /></PreferenceRoot></React.StrictMode>);
  })
  .catch((error) => root.render(<BootError error={error} />));
