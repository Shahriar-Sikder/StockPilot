import React, { useEffect, useRef, useState } from 'react';
import { TOKEN_KEY } from '../api.js';
import { getSetting, L } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import { dismissToast, notify, queueToast, setFormLoading } from '../toast.js';
import Captcha, { CAPTCHA_FIELD } from './captcha.jsx';
import template from './index.html?raw';

const BUSINESS_TYPES = ['Retail', 'Wholesale', 'Grocery', 'Pharmacy', 'Electronics', 'Clothing', 'Restaurant', 'Other'];

// After a request (RequireApprovalForNewTenants): what happens next, the request number and where to check it.
const DONE = '<section class="public-card public-form-card public-done"><span class="public-kicker">{{kicker}}</span><h1>{{title}}</h1>'
  + '<p>{{intro}}</p><div class="public-request-no">{{requestNo}}</div><p>{{emailNote}}</p>'
  + '<div class="public-form-links" style="justify-content:center;gap:22px"><a href="#/register-status?requestNo={{requestNoUrl}}">{{statusLink}}</a><a href="#/login">{{signInLink}}</a></div></section>';

/** The JSON of an MVC action; ABP wraps it as { result: {...}, success: true, __abp: true }. */
export async function readMvcJson(response) {
  let result = {};
  try { result = await response.json(); } catch (_) { throw new Error(L('The server returned an unreadable response. Please try again.')); }
  if (result && result.__abp && result.result && typeof result.result === 'object') result = result.result;
  return result;
}

/**
 * "Create a store": store details and the administrator account. With host approval on, the server saves a request
 * and this page shows its number (?requested=1&requestNo=TR-00001); otherwise the store is created and signed in to.
 */
export default function TenantRegistrationPage() {
  const params = new URLSearchParams(window.location.hash.split('?')[1] || '');
  const [options, setOptions] = useState(null);
  const captchaWidget = useRef(null);

  // Whether the security check and host approval are on (RegistrationOptions).
  useEffect(() => {
    fetch('/TenantRegistration/RegistrationOptions', { credentials: 'same-origin', headers: { Accept: 'application/json' } })
      .then(readMvcJson).then((result) => setOptions(result.result || {})).catch(() => setOptions({}));
  }, []);

  if (params.get('requested') === '1') {
    const requestNo = params.get('requestNo') || '';
    return <HtmlView template={DONE} values={{
      kicker: escapeHtml(L('Request received')),
      title: escapeHtml(L('Thank you! Your store is waiting for approval.')),
      intro: escapeHtml(L('Your request number is')),
      requestNo: escapeHtml(requestNo),
      emailNote: escapeHtml(L("We'll email you when your store is approved. Keep the number to check its status.")),
      requestNoUrl: encodeURIComponent(requestNo),
      statusLink: escapeHtml(L('Check the status')),
      signInLink: escapeHtml(L('Sign in'))
    }} />;
  }

  const message = params.get('pending') === '1'
    ? L('Your workspace is registered and is waiting for activation.')
    : params.get('created') === '1' ? L('Your workspace is ready. Sign in to continue.') : '';

  async function submit(event) {
    event.preventDefault();
    const form = event.target.closest('form');
    if (!form) return;
    const values = new FormData(form); const body = new URLSearchParams();
    for (const [key, value] of values.entries()) body.append(key === CAPTCHA_FIELD ? key : `model.${key}`, value);
    if (options?.useCaptcha && !String(values.get(CAPTCHA_FIELD) || '')) {
      notify('error', L('Complete the security check first.'));
      return;
    }
    setFormLoading(form, true, escapeHtml(L('Sending…')));
    const pendingToast = notify('info', L('Sending your request…'), 30000);
    try {
      const response = await fetch('/TenantRegistration/Register', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' }, body });
      const result = await readMvcJson(response);
      const serverError = result.error || result.Error;
      if (!response.ok || result.success === false || serverError) throw new Error(serverError?.message || serverError?.Message || result.message || L('We could not create your workspace. Check the details and try again.'));
      dismissToast(pendingToast);

      if (result.requestNo) {
        // Approval needed: nothing was created yet, so there is nothing to sign in to.
        window.location.hash = `#/register?requested=1&requestNo=${encodeURIComponent(result.requestNo)}`;
        return;
      }

      // No approval needed: the store exists; sign in to it straight away.
      const credentials = Object.fromEntries(values.entries());
      let authResponse;
      let auth = {};
      try {
        authResponse = await fetch('/api/Account/Authenticate', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json', Accept: 'application/json' }, body: JSON.stringify({ tenancyName: credentials.TenancyName, usernameOrEmailAddress: credentials.AdminEmailAddress, password: credentials.AdminPassword }) });
        try { auth = await authResponse.json(); } catch (_) { /* fall through to the sign-in page */ }
      } catch (_) { /* The workspace is already created; continue to sign in instead of retrying registration. */ }
      const token = auth.result || auth.Result;
      if (authResponse?.ok && typeof token === 'string' && token) {
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem('stockpilot.isHost', 'false');
        localStorage.setItem('stockpilot.storeName', credentials.Name);
        queueToast('success', L('Your workspace is ready.'));
        window.location.hash = '#/dashboard';
        window.location.reload();
        return;
      }
      const pendingActivation = result.targetUrl?.includes('pending=1');
      queueToast(pendingActivation ? 'warning' : 'success', L(pendingActivation
        ? 'Workspace created and awaiting activation. You can sign in after it is activated.'
        : 'Workspace created. Sign in with your workspace name and administrator account.'));
      window.location.assign(`/react/index.html#/login?registered=1&tenancyName=${encodeURIComponent(credentials.TenancyName)}`);
    } catch (e) {
      dismissToast(pendingToast);
      setFormLoading(form, false);
      // An answer can be used only once: show a fresh security check.
      if (captchaWidget.current !== null && window.grecaptcha) window.grecaptcha.reset(captchaWidget.current);
      notify('error', e.message || L('Workspace registration is unavailable right now.'));
    }
  }

  const t = (key) => escapeHtml(L(key));
  const requireApproval = options && typeof options.requireApproval === 'boolean'
    ? options.requireApproval
    : String(getSetting('App.TenantManagement.RequireApprovalForNewTenants') ?? 'true').toLowerCase() === 'true';
  return <HtmlView template={template} onSubmit={submit} values={{
    kicker: t('Get started'), title: t('Create your store'),
    intro: t(requireApproval ? 'Tell us about your store. We review every request and email you when your store is ready.' : 'Tell us about your store and choose the administrator account.'),
    messageBlock: message ? `<div class="public-form-message" role="status">${escapeHtml(message)}</div>` : '',
    storeGroup: t('Store details'), storeName: t('Store name'), storeNameHint: t('Your business name'),
    workspace: t('Workspace name'), workspaceHint: t('Use letters, numbers, dashes, or underscores.'), tenancyName: escapeHtml(params.get('tenancyName') || ''),
    contactName: t('Contact person'), contactPhone: t('Contact phone'), contactEmail: t('Contact email'),
    businessType: t('Business type'), businessTypeHint: t('e.g. Retail'),
    businessTypeOptions: BUSINESS_TYPES.map((type) => `<option value="${escapeHtml(L(type))}"></option>`).join(''),
    address: t('Address'), city: t('City'), country: t('Country'), notes: t('Anything we should know?'), notesHint: t('Optional'),
    adminGroup: t('Administrator account'), adminName: t('First name'), adminSurname: t('Last name'), adminEmail: t('Email'), adminPhone: t('Mobile number'),
    adminPassword: t('Password'), passwordHint: t('Use at least 6 characters, including a lowercase letter and a number.'),
    submit: t(requireApproval ? 'Send request' : 'Create workspace'), signInLink: t('Already have an account? Sign in'), statusLink: t('Check a request')
  }}>
    {options?.useCaptcha && <Captcha siteKey={options.captchaSiteKey} onReady={(widgetId) => { captchaWidget.current = widgetId; }} />}
  </HtmlView>;
}
