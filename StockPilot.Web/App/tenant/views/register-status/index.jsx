import React, { useState } from 'react';
import { L } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import { notify, setFormLoading } from '../toast.js';
import { readMvcJson } from '../tenant-registration/index.jsx';
import template from './index.html?raw';

// TenantRegistrationRequest status -> badge class and label.
const STATUS = {
  0: ['pending', 'Waiting for approval'],
  1: ['approved', 'Approved'],
  2: ['rejected', 'Not approved'],
  3: ['cancelled', 'Cancelled']
};

/** "Check a request": the status of a store request, by request number and the email address used. */
export default function RegisterStatusPage() {
  const params = new URLSearchParams(window.location.hash.split('?')[1] || '');
  const [status, setStatus] = useState(null);

  async function submit(event) {
    event.preventDefault();
    const form = event.target.closest('form');
    if (!form) return;
    const values = Object.fromEntries(new FormData(form).entries());
    setFormLoading(form, true, escapeHtml(L('Checking…')));
    setStatus(null);
    try {
      const query = new URLSearchParams({ requestNo: values.requestNo.trim(), email: values.email.trim() });
      const response = await fetch(`/TenantRegistration/RequestStatus?${query}`, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
      const result = await readMvcJson(response);
      if (!response.ok || result.success === false) throw new Error(result.error?.message || L('No store request matches this request number and email address.'));
      setStatus(result.result);
    } catch (error) {
      notify('error', error.message);
    } finally {
      setFormLoading(form, false);
    }
  }

  const t = (key) => escapeHtml(L(key));
  return <HtmlView template={template} onSubmit={submit} values={{
    kicker: t('Store request'), title: t('Check your request'), intro: t('Enter the request number and the email address you used.'),
    requestNoLabel: t('Request number'), requestNo: escapeHtml(params.get('requestNo') || ''), emailLabel: t('Email'),
    check: t('Check status'), newRequest: t('Create a store'), signIn: t('Sign in')
  }}>
    {status && <RequestStatus status={status} />}
  </HtmlView>;
}

function RequestStatus({ status }) {
  const [badge, label] = STATUS[status.status] || STATUS[0];
  return <div className="public-status" role="status">
    <div><b>{status.requestNo}</b> <span className={`public-badge ${badge}`}>{L(label)}</span></div>
    {status.status === 0 && <div>{L("We'll email you when your store is approved.")}</div>}
    {status.status === 1 && <>
      <div>{L('Your store is ready. Sign in with the store name {0} and the password you chose.', status.tenancyName)}</div>
      <a className="public-button" style={{ justifySelf: 'start' }} href={`#/login?tenancyName=${encodeURIComponent(status.tenancyName || '')}`}>{L('Sign in')}</a>
    </>}
    {status.status === 2 && <div><b>{L('Reason')}:</b> {status.reviewNote || '—'}</div>}
  </div>;
}
