// The only module that touches ABP globals (window.abp and the jQuery proxies).
//
// Host page (dashboard/index.html) loads jquery, abp.js and abp.jquery.js as classic scripts.
// initAbp() then fetches the per-user dynamic scripts with the same bearer token the app signs in with
// (stockpilot.accessToken in localStorage) and runs them:
//   /AbpScripts/GetScripts                     abp.session, abp.auth, abp.localization, abp.setting, abp.features
//   /api/AbpServiceProxies/GetAll?type=jquery  abp.services.app.* (one function per app service method)
// A plain <script src> cannot send the Authorization header, so it would load the anonymous version.

import { TOKEN_KEY } from './api.js';

const SOURCE = 'StockPilot';

function abp() {
  if (!window.abp || !window.abp.ajax) throw new Error('ABP scripts are not loaded (abp.js / abp.jquery.js).');
  return window.abp;
}

/* ---------- bootstrap ---------- */

let ready = null;

/** Loads the dynamic ABP scripts once. Resolves when abp.session / abp.auth / abp.services are filled. */
export function initAbp() {
  if (!ready) ready = loadDynamicScripts();
  return ready;
}

async function loadDynamicScripts() {
  const a = abp();
  const token = getToken();
  if (token) a.ajax.defaultOpts.headers = { ...a.ajax.defaultOpts.headers, Authorization: `Bearer ${token}` };

  // An expired or revoked token: forget it and go to the sign-in page instead of ABP's default redirect to "/".
  a.ajax.handleUnAuthorizedRequest = () => {
    clearToken();
    window.location.hash = '#/login';
    window.location.reload();
  };

  // Every abp.ajax request (callService or a direct proxy call) counts for the top progress bar.
  window.jQuery(document).ajaxSend(() => changePending(1)).ajaxComplete(() => changePending(-1));

  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const [scripts, proxies] = await Promise.all([
    fetchScript(`/AbpScripts/GetScripts?v=${Date.now()}`, headers),
    fetchScript('/api/AbpServiceProxies/GetAll?type=jquery', headers)
  ]);
  runScript(scripts);
  runScript(proxies);

  // A token the server no longer accepts comes back as an anonymous session.
  if (token && !a.session.userId) clearToken();
  return a;
}

async function fetchScript(url, headers) {
  const response = await fetch(url, { credentials: 'same-origin', headers });
  if (!response.ok) throw new Error(`Could not load ${url} (${response.status}).`);
  return response.text();
}

function runScript(code) {
  const script = document.createElement('script');
  script.textContent = code;
  document.head.appendChild(script);
  script.remove();
}

/* ---------- token (sign-in state) ---------- */

export function getToken() {
  try { return localStorage.getItem(TOKEN_KEY); } catch (_) { return null; }
}

export function clearToken() {
  try {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem('stockpilot.user');
    localStorage.removeItem('stockpilot.isHost');
  } catch (_) { /* storage may be blocked */ }
}

/* ---------- app services ---------- */

const camelCase = (name) => String(name).charAt(0).toLowerCase() + String(name).slice(1);

let pending = 0;
const pendingListeners = new Set();
function changePending(delta) {
  pending = Math.max(0, pending + delta);
  pendingListeners.forEach((listener) => listener(pending));
}

/** Subscribes to the number of running requests (for the top progress bar). Returns an unsubscribe function. */
export function onPendingRequests(listener) {
  pendingListeners.add(listener);
  listener(pending);
  return () => pendingListeners.delete(listener);
}

/**
 * Calls an app service through its ABP jQuery proxy and returns a native Promise of the result.
 *   callService('ItemMaster', 'GetAll', input)  ->  abp.services.app.itemMaster.getAll(input)
 * abp.ajax already shows server errors (UserFriendlyException via abp.message.error), so callers must not show
 * a second message; the rejection carries { message, details, status, handled: true }.
 */
export function callService(serviceName, methodName, input) {
  const a = abp();
  const service = a.services && a.services.app ? a.services.app[camelCase(serviceName)] : null;
  const method = service ? service[camelCase(methodName)] : null;
  if (typeof method !== 'function') {
    const error = new Error(`The server has no ${serviceName}.${methodName} method.`);
    a.message.error(error.message);
    error.handled = true;
    return Promise.reject(error);
  }

  return new Promise((resolve, reject) => {
    // Generated proxies are (input, ajaxParams) for methods with a DTO and (ajaxParams) for methods without one.
    const request = method.length >= 2 ? method(input === undefined ? {} : input) : method();
    request
      .done((result) => resolve(result))
      .fail((first, second) => reject(toError(first, second)));
  });
}

function toError(first, second) {
  // ABP error: (error, jqXHR). Transport error: (jqXHR, textStatus, errorThrown).
  const isAbpError = first && typeof first === 'object' && !('readyState' in first);
  const error = new Error(isAbpError ? (first.message || 'An error has occurred!') : 'The request failed.');
  error.details = isAbpError ? first.details : undefined;
  error.validationErrors = isAbpError ? first.validationErrors : undefined;
  error.status = isAbpError ? (second && second.status) : first && first.status;
  error.handled = true;
  return error;
}

/* ---------- permissions ---------- */

export const isGranted = (permissionName) => !!permissionName && abp().auth.isGranted(permissionName);

export const isAnyGranted = (...permissionNames) => abp().auth.isAnyGranted(...permissionNames.flat());

/** Every permission granted to the signed-in user (for pages that take a list). */
export function grantedPermissionNames() {
  const granted = abp().auth.grantedPermissions || {};
  return Object.keys(granted).filter((name) => granted[name]);
}

/** abp.features.isEnabled: true for a checkbox feature whose value is "true" (from /AbpScripts/GetScripts). */
export const isFeatureEnabled = (featureName) => !!(abp().features && abp().features.isEnabled(featureName));

/** The workspace's feature values from abp.features ({ 'App.MaxUserCount': '0', … }). Read-only: the host sets them. */
export function getFeatureValues() {
  const all = (abp().features && abp().features.allFeatures) || {};
  return Object.fromEntries(Object.entries(all).map(([name, feature]) => [name, feature && feature.value]));
}

/* ---------- localization ---------- */

/**
 * UI text from the StockPilot source. Keys for UI strings are the English text itself (as in the mockup), so a
 * missing key still shows readable English. Extra arguments fill {0}, {1}, …
 */
export function L(key, ...args) {
  const a = abp();
  const text = a.localization.localize(key, SOURCE);
  return args.length ? a.utils.formatString(text, ...args) : text;
}

export const currentLanguage = () => {
  const language = abp().localization.currentLanguage;
  return (language && language.name) || 'en';
};

/** Languages enabled on the server: [{ name, displayName, icon, isDefault }]. */
export const getLanguages = () => abp().localization.languages || [];

/** Switches the ABP culture (cookie read by the server on every request) and reloads so all texts reload. */
export function setLanguage(cultureName) {
  const a = abp();
  a.utils.setCookieValue('Abp.Localization.CultureName', cultureName, new Date(new Date().getTime() + 5 * 365 * 86400000), a.appPath);
  window.location.reload();
}

/* ---------- session ---------- */

/** abp.session: { userId, tenantId, impersonatorUserId, impersonatorTenantId, multiTenancySide }. */
export const getSession = () => abp().session || {};

/** A setting visible to clients (abp.setting, from /AbpScripts/GetScripts; also for anonymous pages). */
export const getSetting = (name) => (abp().setting && abp().setting.get ? abp().setting.get(name) : undefined);

export const isSignedIn = () => !!getSession().userId;

/* ---------- messages, notifications, busy state ---------- */

export const notifySuccess = (message, title) => abp().notify.success(message, title);
export const notifyInfo = (message, title) => abp().notify.info(message, title);
export const notifyWarn = (message, title) => abp().notify.warn(message, title);
export const notifyError = (message, title) => abp().notify.error(message, title);

/** Resolves true when the user confirms. */
export function confirm(message, title) {
  return new Promise((resolve) => abp().message.confirm(message, title, (confirmed) => resolve(!!confirmed)));
}

/** Busy state for one element (a button shows a spinner and the text, e.g. "Posting…"), or the whole page when no element is given. */
export const setBusy = (element, busyText) => abp().ui.setBusy(element, busyText === undefined ? undefined : { busyText });
export const clearBusy = (element) => abp().ui.clearBusy(element);

/**
 * Plugs the mockup's UI into ABP's notification, message and busy APIs (abp.js only ships stubs that log
 * "not implemented"). Called once by the shell with its toast and dialog functions.
 */
export function installUi({ toast, alert, confirmDialog }) {
  const a = abp();
  ['success', 'info', 'warn', 'error'].forEach((type) => {
    a.notify[type] = (message, title, options) => toast(type, title ? `${title}: ${message}` : message, options);
  });

  ['info', 'success', 'warn', 'error'].forEach((type) => {
    a.message[type] = (message, title) => {
      const done = window.jQuery.Deferred();
      alert({ type, message, title }).then(() => done.resolve());
      return done.promise();
    };
  });

  a.message.confirm = (message, titleOrCallback, callback) => {
    const title = typeof titleOrCallback === 'string' ? titleOrCallback : undefined;
    const done = typeof titleOrCallback === 'function' ? titleOrCallback : callback;
    const deferred = window.jQuery.Deferred();
    confirmDialog({ message, title }).then((confirmed) => {
      if (done) done(confirmed);
      deferred.resolve(confirmed);
    });
    return deferred.promise();
  };

  let pageBusy = 0;
  a.ui.setBusy = (element, options) => {
    const node = element && element.jquery ? element[0] : element;
    if (!node) { pageBusy += 1; changePending(1); return; }
    node.classList.add('is-busy');
    node.setAttribute('aria-busy', 'true');
    node.dataset.busyText = (options && options.busyText) || node.textContent.trim();
    if ('disabled' in node) node.disabled = true;
  };
  a.ui.clearBusy = (element) => {
    const node = element && element.jquery ? element[0] : element;
    if (!node) { if (pageBusy > 0) { pageBusy -= 1; changePending(-1); } return; }
    node.classList.remove('is-busy');
    node.removeAttribute('aria-busy');
    delete node.dataset.busyText;
    if ('disabled' in node) node.disabled = false;
  };
}
