import { useEffect, useRef, useState } from 'react';
import { callService, clearBusy, getFeatureValues, isGranted, L, notifySuccess, setBusy } from '../abp.js';
import { n } from '../inventory-shared/format.js';

/**
 * Settings as a form: TenantSettings (or HostSettings for the host) GetAllSettings / UpdateAllSettings, one pill per
 * section of the settings DTO. A section shows only when the server sends it (Email and LDAP are single-tenant only,
 * General only with multiple time zones). The whole DTO is sent back on save, so fields not shown keep their values.
 *
 * Features are read-only: the host grants them to the workspace (TenantAppService.UpdateTenantFeatures needs the host
 * permission Pages.Tenants.ChangeFeatures), so they come from abp.features.
 */
const COMPLEXITY = (field) => (d) => `security.${d.security.useDefaultPasswordComplexitySettings ? 'defaultPasswordComplexity' : 'passwordComplexity'}.${field}`;
const defaultComplexity = (d) => d.security.useDefaultPasswordComplexitySettings;

const SECTIONS = [
  {
    key: 'general', label: 'General', show: (d) => !!d.general,
    groups: [{ title: 'Time zone', sub: 'Dates and times are shown in this time zone. Leave it on the default to follow the server.', fields: [{ path: 'general.timezone', label: 'Time zone', type: 'timezone' }] }]
  },
  {
    key: 'users', label: 'User management', show: (d) => !!d.userManagement,
    groups: [{
      title: 'Sign-up and sign-in', sub: 'How new users join this workspace and what they need before they can sign in.',
      fields: [
        { path: 'userManagement.allowSelfRegistration', label: 'Allow users to register themselves', type: 'bool', show: (d) => 'allowSelfRegistration' in d.userManagement },
        { path: 'userManagement.isNewRegisteredUserActiveByDefault', label: 'New registered users are active by default', type: 'bool', show: (d) => 'isNewRegisteredUserActiveByDefault' in d.userManagement },
        { path: 'userManagement.useCaptchaOnRegistration', label: 'Use a security check (captcha) on registration', type: 'bool', show: (d) => 'useCaptchaOnRegistration' in d.userManagement },
        { path: 'userManagement.isEmailConfirmationRequiredForLogin', label: 'Email address must be confirmed to sign in', type: 'bool' }
      ]
    }]
  },
  {
    key: 'tenants', label: 'Workspaces', show: (d) => !!d.tenantManagement,
    groups: [{
      title: 'Workspace registration', sub: 'How new workspaces are created.',
      fields: [
        { path: 'tenantManagement.allowSelfRegistration', label: 'Allow workspaces to register themselves', type: 'bool' },
        { path: 'tenantManagement.isNewRegisteredTenantActiveByDefault', label: 'New workspaces are active by default', type: 'bool' },
        { path: 'tenantManagement.useCaptchaOnRegistration', label: 'Use a security check (captcha) on registration', type: 'bool' },
        { path: 'tenantManagement.requireApprovalForNewTenants', label: 'New stores wait for host approval', type: 'bool', wide: true }
      ]
    }, {
      title: 'Host sign-in', sub: 'Where host users sign in. A separate page keeps the store name required on the normal sign-in page.',
      fields: [{
        path: 'tenantManagement.hostLoginMode', label: 'Host users sign in on', type: 'select',
        options: [['SeparatePage', 'A separate host sign-in page'], ['LoginPage', 'The normal sign-in page (blank store name)']]
      }]
    }]
  },
  {
    key: 'security', label: 'Security', show: (d) => !!d.security,
    groups: [
      {
        title: 'Password rules', sub: 'What a new password must contain.',
        fields: [
          { path: 'security.useDefaultPasswordComplexitySettings', label: 'Use the default rules', type: 'bool', wide: true },
          { path: COMPLEXITY('minLength'), label: 'Minimum length', type: 'number', min: 1, disabled: defaultComplexity },
          { path: COMPLEXITY('maxLength'), label: 'Maximum length', type: 'number', min: 1, disabled: defaultComplexity },
          { path: COMPLEXITY('useNumbers'), label: 'Needs a number', type: 'bool', disabled: defaultComplexity },
          { path: COMPLEXITY('useUpperCaseLetters'), label: 'Needs an upper-case letter', type: 'bool', disabled: defaultComplexity },
          { path: COMPLEXITY('useLowerCaseLetters'), label: 'Needs a lower-case letter', type: 'bool', disabled: defaultComplexity },
          { path: COMPLEXITY('usePunctuations'), label: 'Needs a symbol', type: 'bool', disabled: defaultComplexity }
        ]
      },
      {
        title: 'Account lock', sub: 'Lock an account for a while after too many wrong passwords.',
        fields: [
          { path: 'security.userLockOut.isEnabled', label: 'Lock accounts after failed sign-ins', type: 'bool', wide: true },
          { path: 'security.userLockOut.maxFailedAccessAttemptsBeforeLockout', label: 'Failed attempts before locking', type: 'number', min: 1, disabled: (d) => !d.security.userLockOut.isEnabled },
          { path: 'security.userLockOut.defaultAccountLockoutSeconds', label: 'Lock for (seconds)', type: 'number', min: 1, disabled: (d) => !d.security.userLockOut.isEnabled }
        ]
      },
      {
        title: 'Two-factor sign-in', sub: 'Ask for a second code after the password.',
        show: (d, host) => host || (d.security.twoFactorLogin && d.security.twoFactorLogin.isEnabledForApplication),
        fields: [
          { path: 'security.twoFactorLogin.isEnabled', label: 'Turn on two-factor sign-in', type: 'bool', wide: true },
          { path: 'security.twoFactorLogin.isEmailProviderEnabled', label: 'Send the code by email', type: 'bool', show: (d, host) => host, disabled: (d) => !d.security.twoFactorLogin.isEnabled },
          { path: 'security.twoFactorLogin.isSmsProviderEnabled', label: 'Send the code by SMS', type: 'bool', show: (d, host) => host, disabled: (d) => !d.security.twoFactorLogin.isEnabled },
          { path: 'security.twoFactorLogin.isRememberBrowserEnabled', label: 'Let users remember this browser', type: 'bool', disabled: (d) => !d.security.twoFactorLogin.isEnabled }
        ]
      }
    ]
  },
  {
    key: 'email', label: 'Email', show: (d) => !!d.email,
    groups: [
      {
        title: 'Sender', sub: 'The address and name emails are sent from.',
        fields: [
          { path: 'email.defaultFromAddress', label: 'From address', type: 'email' },
          { path: 'email.defaultFromDisplayName', label: 'From name', type: 'text' }
        ]
      },
      {
        title: 'SMTP server', sub: 'The mail server that sends the emails.',
        fields: [
          { path: 'email.smtpHost', label: 'Host', type: 'text' },
          { path: 'email.smtpPort', label: 'Port', type: 'number', min: 1 },
          { path: 'email.smtpEnableSsl', label: 'Use SSL', type: 'bool' },
          { path: 'email.smtpUseDefaultCredentials', label: 'Use default credentials', type: 'bool' },
          { path: 'email.smtpDomain', label: 'Domain', type: 'text', disabled: (d) => d.email.smtpUseDefaultCredentials },
          { path: 'email.smtpUserName', label: 'User name', type: 'text', disabled: (d) => d.email.smtpUseDefaultCredentials },
          { path: 'email.smtpPassword', label: 'Password', type: 'password', disabled: (d) => d.email.smtpUseDefaultCredentials }
        ]
      }
    ]
  },
  {
    key: 'ldap', label: 'LDAP', show: (d) => !!(d.ldap && d.ldap.isModuleEnabled),
    groups: [{
      title: 'LDAP sign-in', sub: 'Let users sign in with their directory account.',
      fields: [
        { path: 'ldap.isEnabled', label: 'Turn on LDAP sign-in', type: 'bool', wide: true },
        { path: 'ldap.domain', label: 'Domain', type: 'text', disabled: (d) => !d.ldap.isEnabled },
        { path: 'ldap.userName', label: 'User name', type: 'text', disabled: (d) => !d.ldap.isEnabled },
        { path: 'ldap.password', label: 'Password', type: 'password', disabled: (d) => !d.ldap.isEnabled }
      ]
    }]
  },
  {
    key: 'images', label: 'Image storage', show: (d) => !!d.ecomm,
    groups: [{
      title: 'Pictures', sub: 'Where item and category pictures are kept.',
      fields: [
        { path: 'ecomm.willUseServerImage', label: 'Keep pictures on an image server', type: 'bool' },
        { path: 'ecomm.willUseCloudinaryImage', label: 'Keep pictures on Cloudinary', type: 'bool' },
        { path: 'ecomm.imageServerUrl', label: 'Image server address', type: 'text', disabled: (d) => !d.ecomm.willUseServerImage },
        { path: 'ecomm.webSiteAddress', label: 'Website address', type: 'text' }
      ]
    }]
  },
  { key: 'features', label: 'Features', show: () => true }
];

// Feature names defined in AppFeatureProvider, with how to show their value. Others show name and value as they are.
const FEATURES = {
  'App.MaxUserCount': { label: 'Maximum user count', value: (v) => (Number(v) > 0 ? n(v, 0) : L('Unlimited')) },
  'App.HideCostAndValues': { label: 'HideCostAndValues', value: (v) => (isOn(v) ? L('Yes') : L('No')) }
};

const isOn = (value) => String(value).toLowerCase() === 'true';
const resolve = (path, data) => (typeof path === 'function' ? path(data) : path);
const read = (data, path) => path.split('.').reduce((value, key) => (value == null ? value : value[key]), data);
function write(data, path, value) {
  const keys = path.split('.');
  const copy = { ...data };
  let target = copy;
  keys.slice(0, -1).forEach((key) => { target[key] = { ...target[key] }; target = target[key]; });
  target[keys[keys.length - 1]] = value;
  return copy;
}

export default function SettingsPage() {
  const host = isGranted('Pages.Administration.Host.Settings');
  const service = host ? 'HostSettings' : 'TenantSettings';
  const [saved, setSaved] = useState(null);
  const [data, setData] = useState(null);
  const [tab, setTab] = useState('users');
  const [timezones, setTimezones] = useState([]);
  const saveButton = useRef(null);

  async function load() {
    try {
      const result = await callService(service, 'GetAllSettings');
      setSaved(result); setData(result);
    } catch (_) { setData((current) => current || {}); /* abp.ajax already showed the error */ }
  }
  useEffect(() => { load(); }, [service]);

  useEffect(() => {
    if (!saved || !saved.general) return;
    callService('Timing', 'GetTimezoneComboboxItems', { defaultTimezoneScope: host ? 1 : 2, selectedTimezoneId: saved.general.timezone })
      .then((items) => setTimezones(items || [])).catch(() => {});
  }, [saved]);

  const sections = data ? SECTIONS.filter((s) => s.show(data, host)) : [];
  const current = sections.find((s) => s.key === tab) || sections[0];
  const dirty = !!saved && JSON.stringify(saved) !== JSON.stringify(data);

  async function save(event) {
    event.preventDefault();
    setBusy(saveButton.current);
    try {
      await callService(service, 'UpdateAllSettings', data);
      setSaved(data);
      notifySuccess(L('Settings saved.'));
    } catch (_) { /* abp.ajax already showed the error */ } finally { clearBusy(saveButton.current); }
  }

  return <form onSubmit={save}>
    <div className="hd">
      <div><h1>{L('Settings')}</h1><p className="sub">{L(host ? 'Settings for the whole application.' : 'Settings for this workspace.')}<span className="tag">{service}</span></p></div>
      <div className="st-act">
        {dirty && <span className="mu">{L('Unsaved changes')}</span>}
        <button type="button" className="btn s" disabled={!dirty} onClick={() => setData(saved)}>{L('Discard')}</button>
        <button type="submit" className="btn" ref={saveButton} disabled={!dirty}>{L('Save settings')}</button>
      </div>
    </div>
    {!data && <div className="card">{[0, 1, 2, 3].map((i) => <div key={i} className="sk" style={{ height: 14, margin: '12px 0' }} />)}</div>}
    {data && <>
      <div className="pills">{sections.map((s) => <button type="button" key={s.key} className={current && current.key === s.key ? 'on' : ''} onClick={() => setTab(s.key)}>{L(s.label)}</button>)}</div>
      {current && current.key === 'features'
        ? <FeaturesCard />
        : current && current.groups.filter((g) => !g.show || g.show(data, host)).map((group) => <div className="card st-card" key={group.title}>
          <h3>{L(group.title)}</h3>
          <p className="sub">{L(group.sub)}</p>
          <div className="fm st-fm">{group.fields.filter((f) => !f.show || f.show(data, host)).map((field) => <Field key={field.label} field={field} data={data} timezones={timezones} onChange={(path, value) => setData((d) => write(d, path, value))} />)}</div>
        </div>)}
    </>}
  </form>;
}

function Field({ field, data, timezones, onChange }) {
  const path = resolve(field.path, data);
  const value = read(data, path);
  const disabled = field.disabled ? !!field.disabled(data) : false;
  if (field.type === 'bool') {
    return <label className={`ck${field.wide ? ' st-wide' : ''}`}><input type="checkbox" checked={!!value} disabled={disabled} onChange={(e) => onChange(path, e.target.checked)} />{L(field.label)}</label>;
  }
  if (field.type === 'select') {
    return <label>{L(field.label)}<select value={value || ''} disabled={disabled} onChange={(e) => onChange(path, e.target.value)}>
      {field.options.map(([option, label]) => <option key={option} value={option}>{L(label)}</option>)}
    </select></label>;
  }
  if (field.type === 'timezone') {
    return <label>{L(field.label)}<select value={value || ''} onChange={(e) => onChange(path, e.target.value)}>
      {!timezones.some((t) => t.value === '') && <option value="">{L('Default')}</option>}
      {timezones.map((t) => <option key={t.value} value={t.value}>{t.displayText}</option>)}
    </select></label>;
  }
  return <label>{L(field.label)}<input type={field.type} min={field.min} disabled={disabled} autoComplete={field.type === 'password' ? 'new-password' : undefined}
    value={value ?? ''} onChange={(e) => onChange(path, field.type === 'number' ? (e.target.value === '' ? '' : Number(e.target.value)) : e.target.value)} /></label>;
}

function FeaturesCard() {
  const values = getFeatureValues();
  const names = Object.keys(values);
  return <div className="card st-card">
    <h3>{L('Features')}</h3>
    <p className="sub">{L('What this workspace is allowed to use. Only the host can change these.')}</p>
    {names.length
      ? <div className="dl">{names.map((name) => {
        const known = FEATURES[name];
        const raw = values[name];
        const shown = known ? known.value(raw) : raw === 'true' ? L('Yes') : raw === 'false' ? L('No') : raw;
        return <div key={name}><span>{known ? L(known.label) : name}</span><b>{shown}</b></div>;
      })}</div>
      : <div className="empty"><b>{L('No features')}</b>{L('No features are defined for this workspace.')}</div>}
  </div>;
}
