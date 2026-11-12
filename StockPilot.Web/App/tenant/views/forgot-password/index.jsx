import React from 'react';
import { HtmlView } from '../template.jsx';
import { notify, setFormLoading } from '../toast.js';
import template from './index.html?raw';

/** Sends the password reset link (Account/SendPasswordResetLink, an MVC action). */
export default function ForgotPasswordPage() {
  async function submit(event) {
    event.preventDefault();
    const form = event.target.closest('form');
    if (!form) return;
    const values = new FormData(form); const body = new URLSearchParams();
    for (const [key, value] of values.entries()) body.append(`model.${key}`, value);
    setFormLoading(form, true, 'Sending…');
    try {
      const response = await fetch('/Account/SendPasswordResetLink', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' }, body });
      const result = await response.json();
      if (!response.ok || result.success === false || result.error) throw new Error(result.error?.message || result.error?.Message || 'We could not send the reset link.');
      setFormLoading(form, false);
      notify('success', 'If an account matches that email, a reset link is on its way.');
    } catch (error) {
      setFormLoading(form, false);
      notify('error', error.message || 'Password reset is unavailable right now.');
    }
  }

  return <HtmlView template={template} values={{ messageBlock: '' }} onSubmit={submit} />;
}
