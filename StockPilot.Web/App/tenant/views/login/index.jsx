import { TOKEN_KEY } from '../api.js';
import { currentLanguage, getLanguages, getSetting, L, setLanguage } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import { notify, queueToast, setFormLoading } from '../toast.js';
import template from './index.html?raw';

const FALLBACK_LANGUAGES = [{ name: 'en', displayName: 'English' }, { name: 'bn', displayName: 'বাংলা' }, { name: 'hi', displayName: 'हिन्दी' }];

/** App.TenantManagement.HostLoginMode: 'SeparatePage' (default) moves host sign-in to #/host-login. */
export const hostSignInIsSeparate = () => (getSetting('App.TenantManagement.HostLoginMode') || 'SeparatePage') === 'SeparatePage';

/** The language buttons under the card. */
export function languageButtons() {
  return (getLanguages().length ? getLanguages() : FALLBACK_LANGUAGES)
    .map((entry) => `<button type="button" data-language="${escapeHtml(entry.name)}" class="${entry.name === currentLanguage() ? 'on' : ''}">${escapeHtml(entry.displayName || entry.name)}</button>`).join('');
}

export function languageClick(event) {
  const language = event.target.closest('[data-language]')?.dataset.language;
  if (language) { event.preventDefault(); setLanguage(language); }
}

/**
 * The token sign-in (POST /api/Account/Authenticate, not an app service, so it stays on fetch). host = true is the
 * host sign-in page: no store name, and hostLogin: true so the server accepts the blank store name. After sign-in
 * the page reloads so abp.js fetches the user's session, permissions and texts with the new token.
 */
export async function signIn(event, { host = false } = {}) {
  event.preventDefault();
  const form = event.target.closest('form');
  if (!form) return;
  const credentials = Object.fromEntries(new FormData(form).entries());
  const tenancyName = host ? null : (credentials.TenancyName || '').trim() || null;
  setFormLoading(form, true, escapeHtml(L('Signing in…')));
  try {
    const response = await fetch('/api/Account/Authenticate', {
      method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ tenancyName, usernameOrEmailAddress: credentials.UsernameOrEmailAddress, password: credentials.Password, hostLogin: host })
    });
    const result = await response.json();
    const serverError = result.error || result.Error;
    if (!response.ok || result.success === false || serverError) throw new Error(serverError?.details || serverError?.message || L('We could not sign you in. Check your workspace name and account details, then try again.'));
    const token = result.result || result.Result;
    if (!token) throw new Error(L('Sign in succeeded but the server did not return an access token.'));
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem('stockpilot.isHost', tenancyName ? 'false' : 'true');
    queueToast('success', L('Signed in successfully.'));
    window.location.hash = '#/dashboard';
    window.location.reload();
  } catch (error) {
    setFormLoading(form, false);
    notify('error', error.message || L('Sign in is unavailable right now.'));
  }
}

/**
 * The store sign-in, restyled as the mockup's login card. With a separate host sign-in page the store name is
 * required and a small "Host sign in" link leads to #/host-login; otherwise a blank store name signs in as host.
 */
export default function LoginPage() {
  const params = new URLSearchParams(window.location.hash.split('?')[1] || '');
  const message = params.get('registered') === '1' ? L('Your workspace is registered. Check your email if activation is required, then sign in with your workspace name.') : '';
  const separate = hostSignInIsSeparate();

  const values = {
    welcome: escapeHtml(L('Welcome back')),
    subtitle: escapeHtml(L('Sign in to your workspace')),
    messageBlock: message ? `<p class="mu" role="status" style="margin:0">${escapeHtml(message)}</p>` : '',
    tenantLabel: escapeHtml(L('Tenant')),
    tenantHint: escapeHtml(separate ? L('Your store name') : L('Leave blank for the host administrator')),
    tenantRequired: separate ? 'required' : '',
    tenancyName: escapeHtml(params.get('tenancyName') || ''),
    userLabel: escapeHtml(L('Username')),
    passwordLabel: escapeHtml(L('Password')),
    signIn: escapeHtml(L('Sign in')),
    languages: languageButtons(),
    createStore: escapeHtml(L('Create a store')),
    hostLink: separate ? `<p class="mu" style="margin:2px 0 0;text-align:center;font-size:12px"><a href="#/host-login" style="color:var(--mu)">${escapeHtml(L('Host sign in'))}</a></p>` : ''
  };
  return <HtmlView template={template} values={values} onSubmit={(event) => signIn(event)} onClick={languageClick} />;
}
