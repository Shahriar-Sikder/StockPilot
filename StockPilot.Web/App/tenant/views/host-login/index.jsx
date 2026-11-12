import React from 'react';
import { L } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import { languageButtons, languageClick, signIn } from '../login/index.jsx';
import template from './index.html?raw';

/** The host sign-in page (App.TenantManagement.HostLoginMode = SeparatePage): no store name. */
export default function HostLoginPage() {
  const values = {
    title: escapeHtml(L('Host sign in')),
    subtitle: escapeHtml(L('For the people who run StockPilot. Store users sign in on the normal page.')),
    userLabel: escapeHtml(L('Username')),
    passwordLabel: escapeHtml(L('Password')),
    signIn: escapeHtml(L('Sign in')),
    languages: languageButtons(),
    storeLink: escapeHtml(L('Sign in to a store'))
  };
  return <HtmlView template={template} values={values} onSubmit={(event) => signIn(event, { host: true })} onClick={languageClick} />;
}
