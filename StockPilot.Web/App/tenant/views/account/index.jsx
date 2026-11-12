import { useEffect, useMemo, useRef, useState } from 'react';
import { callService, L, notifySuccess } from '../abp.js';
import { avatarColour } from '../inventory-shared/avatar.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import Icon from '../inventory-shared/icons.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { notify } from '../toast.js';
import './account.css';

/*
 * My account (#/account?section=…), opened from the profile menu: Profile (my-settings), Picture (profile-picture),
 * Password (change-password) and Sign-in history (login-attempts). Pictures go to the server as base64
 * (Profile.UploadProfilePicture), cropped and resized here. A change to the name or picture is announced with the
 * window event "stockpilot:profile-changed" so the top bar updates.
 */
const TABS = [['my-settings', 'Profile', 'user'], ['profile-picture', 'Picture', 'camera'], ['change-password', 'Password', 'lock'], ['login-attempts', 'Sign-in history', 'file']];
const tabOf = (section) => (TABS.some(([key]) => key === section) ? section : 'my-settings');
export const profileChanged = () => window.dispatchEvent(new Event('stockpilot:profile-changed'));

/** The signed-in user's picture (data: URL or ''), loaded once and again after "stockpilot:profile-changed". */
export function useProfilePicture() {
  const [src, setSrc] = useState('');
  useEffect(() => {
    let live = true;
    const load = () => callService('Profile', 'GetProfilePicture', {})
      .then((p) => { if (live) setSrc(p && p.base64Content ? `data:${p.contentType};base64,${p.base64Content}` : ''); })
      .catch(() => { /* initials instead */ });
    load();
    window.addEventListener('stockpilot:profile-changed', load);
    return () => { live = false; window.removeEventListener('stockpilot:profile-changed', load); };
  }, []);
  return src;
}

const initials = (text) => String(text || '').split(' ').filter(Boolean).slice(0, 2).map((w) => w[0]).join('').toUpperCase() || '?';

export function ProfileAvatar({ name, src, size = 40, className = '' }) {
  return src
    ? <img className={`pa ${className}`.trim()} src={src} alt="" width={size} height={size} style={{ width: size, height: size }} />
    : <span className={`pa ${className}`.trim()} style={{ width: size, height: size, background: avatarColour(name), fontSize: Math.round(size * 0.38) }}>{initials(name)}</span>;
}

export default function AccountPage({ user, tenantName }) {
  const [tab, setTab] = useState(() => tabOf(new URLSearchParams(window.location.hash.split('?')[1] || '').get('section')));
  useHashQuery((query) => { if (query.get('section')) setTab(tabOf(query.get('section'))); });
  const [profile, setProfile] = useState(null);
  const picture = useProfilePicture();

  useEffect(() => { callService('Profile', 'GetCurrentUserProfileForEdit', {}).then(setProfile).catch(() => setProfile(false)); }, []);
  const fullName = profile ? [profile.name, profile.surname].filter(Boolean).join(' ') : [user?.name, user?.surname].filter(Boolean).join(' ');

  return <>
    <div className="hd"><div><h1>{L('My account')}</h1><p className="sub">{L('Your details, picture, password and recent sign-ins.')}</p></div></div>
    <div className="card ac-head">
      <ProfileAvatar name={fullName || user?.userName} src={picture} size={64} />
      <div className="ac-who">
        <b>{fullName || user?.userName}</b>
        <span className="mu">{profile?.userName || user?.userName}{(profile?.emailAddress || user?.emailAddress) ? ` · ${profile?.emailAddress || user?.emailAddress}` : ''}</span>
        <span className="mu">{tenantName ? L('Store: {0}', tenantName) : L('Host administrator account')}</span>
      </div>
    </div>
    <div className="pills ac-tabs" role="tablist">{TABS.map(([key, label, icon]) => <button type="button" role="tab" aria-selected={tab === key} key={key} className={tab === key ? 'on' : ''} onClick={() => setTab(key)}>
      <Icon name={icon} size={15} />{L(label)}
    </button>)}</div>
    {tab === 'my-settings' && <ProfileTab profile={profile} onSaved={(p) => { setProfile(p); profileChanged(); }} />}
    {tab === 'profile-picture' && <PictureTab name={fullName} current={picture} />}
    {tab === 'change-password' && <PasswordTab />}
    {tab === 'login-attempts' && <SignInsTab />}
  </>;
}

/* ---------- Profile ---------- */

const PROFILE_FIELDS = [['name', 'First name', true, 'text', 'given-name'], ['surname', 'Last name', true, 'text', 'family-name'], ['userName', 'User name', true, 'text', 'username'], ['emailAddress', 'Email', true, 'email', 'email'], ['phoneNumber', 'Mobile number', false, 'tel', 'tel']];

function ProfileTab({ profile, onSaved }) {
  const [model, setModel] = useState(null);
  const [busy, setBusy] = useState(false);
  useEffect(() => { if (profile) setModel(profile); }, [profile]);
  if (profile === null || !model) return profile === false ? <Failed /> : <div className="card"><Skeleton kpis={false} lines={5} /></div>;

  const dirty = PROFILE_FIELDS.some(([key]) => (model[key] ?? '') !== (profile[key] ?? ''));
  async function save(event) {
    event.preventDefault();
    setBusy(true);
    const input = { ...model, ...Object.fromEntries(PROFILE_FIELDS.map(([key]) => [key, String(model[key] ?? '').trim()])) };
    try {
      await callService('Profile', 'UpdateCurrentUserProfile', input);
      notifySuccess(L('Your profile is saved.'));
      onSaved(input);
    } catch (_) { /* shown by abp.ajax */ } finally { setBusy(false); }
  }
  return <form className="card ac-card" onSubmit={save}>
    <h3>{L('Your details')}</h3>
    <p className="mu ac-lead">{L('The name others see on documents and in the audit log, and how we reach you.')}</p>
    <div className="fm">
      {PROFILE_FIELDS.map(([key, label, required, type, auto]) => <label key={key}>
        <span>{L(label)}{required && <span className="req"> *</span>}</span>
        <input type={type} required={required} autoComplete={auto} value={model[key] ?? ''} onChange={(e) => setModel((m) => ({ ...m, [key]: e.target.value }))} />
      </label>)}
    </div>
    <div className="ac-actions">
      <button type="button" className="btn s" disabled={!dirty || busy} onClick={() => setModel(profile)}>{L('Undo changes')}</button>
      <button type="submit" className="btn" disabled={!dirty || busy}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save profile')}</button>
    </div>
  </form>;
}

/* ---------- Picture ---------- */

const VIEW = 240; // crop window (CSS px)
const OUT = 256; // saved picture (px)
const MAX_SOURCE = 10 * 1024 * 1024;

function PictureTab({ name, current }) {
  const [image, setImage] = useState(null); // { el, url, w, h }
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [over, setOver] = useState(false);
  const input = useRef(null);
  const drag = useRef(null);

  // Scale that makes the shorter side fill the window; zoom multiplies it.
  const base = image ? VIEW / Math.min(image.w, image.h) : 1;
  const scale = base * zoom;
  const clamp = (x, y, s = scale) => {
    if (!image) return { x: 0, y: 0 };
    const maxX = Math.max(0, (image.w * s - VIEW) / 2), maxY = Math.max(0, (image.h * s - VIEW) / 2);
    return { x: Math.min(maxX, Math.max(-maxX, x)), y: Math.min(maxY, Math.max(-maxY, y)) };
  };

  function choose(file) {
    if (!file) return;
    if (!/^image\//.test(file.type)) { notify('error', L('Choose an image file (JPG, PNG, GIF or WebP).')); return; }
    if (file.size > MAX_SOURCE) { notify('error', L('The image is larger than 10 MB. Choose a smaller one.')); return; }
    const url = URL.createObjectURL(file);
    const el = new Image();
    el.onload = () => { setImage((old) => { if (old) URL.revokeObjectURL(old.url); return { el, url, w: el.naturalWidth, h: el.naturalHeight }; }); setZoom(1); setOffset({ x: 0, y: 0 }); };
    el.onerror = () => { URL.revokeObjectURL(url); notify('error', L('This file could not be read as an image.')); };
    el.src = url;
  }
  useEffect(() => () => { if (image) URL.revokeObjectURL(image.url); }, [image]);

  function onPointerDown(e) { if (!image) return; e.currentTarget.setPointerCapture(e.pointerId); drag.current = { x: e.clientX, y: e.clientY, start: offset }; }
  function onPointerMove(e) { if (!drag.current) return; setOffset(clamp(drag.current.start.x + e.clientX - drag.current.x, drag.current.start.y + e.clientY - drag.current.y)); }
  function onPointerUp() { drag.current = null; }
  function onWheel(e) { if (!image) return; const z = Math.min(4, Math.max(1, zoom - e.deltaY * 0.0015)); setZoom(z); setOffset((o) => clamp(o.x, o.y, base * z)); }
  function onKey(e) {
    const step = e.shiftKey ? 20 : 5;
    const moves = { ArrowLeft: [step, 0], ArrowRight: [-step, 0], ArrowUp: [0, step], ArrowDown: [0, -step] };
    if (moves[e.key]) { e.preventDefault(); setOffset((o) => clamp(o.x + moves[e.key][0], o.y + moves[e.key][1])); }
  }

  async function save() {
    if (!image) return;
    setBusy(true);
    try {
      const canvas = document.createElement('canvas');
      canvas.width = OUT; canvas.height = OUT;
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, OUT, OUT); // transparent PNGs become white, not black, as JPEG
      // The window shows the image centred, scaled by `scale` and moved by `offset`; draw the same part at OUT px.
      const k = OUT / VIEW;
      const w = image.w * scale * k, h = image.h * scale * k;
      ctx.drawImage(image.el, (OUT - w) / 2 + offset.x * k, (OUT - h) / 2 + offset.y * k, w, h);
      const dataUrl = canvas.toDataURL('image/jpeg', 0.9);
      await callService('Profile', 'UploadProfilePicture', { fileName: 'profile.jpg', base64Content: dataUrl });
      notifySuccess(L('Your picture is saved.'));
      setImage(null);
      profileChanged();
    } catch (_) { /* shown by abp.ajax */ } finally { setBusy(false); }
  }

  const style = image ? { width: image.w * scale, height: image.h * scale, transform: `translate(calc(-50% + ${offset.x}px), calc(-50% + ${offset.y}px))` } : undefined;
  return <div className="card ac-card">
    <h3>{L('Profile picture')}</h3>
    <p className="mu ac-lead">{L('Shown in the top bar and next to your name. Choose a picture, then drag and zoom to frame it.')}</p>
    <div className="ac-pic">
      <div className="ac-pic-now">
        <ProfileAvatar name={name} src={current} size={112} />
        <span className="mu">{L(current ? 'Current picture' : 'No picture yet')}</span>
        {current && !image && <button type="button" className="btn s d" onClick={() => setRemoving(true)}><Icon name="trash" size={15} />{L('Remove picture')}</button>}
      </div>
      <div className="ac-pic-edit">
        {image ? <>
          <div className="ac-crop" tabIndex={0} role="img" aria-label={L('Drag to move the picture; use the arrow keys or the slider to adjust.')}
            onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={onPointerUp} onPointerCancel={onPointerUp} onWheel={onWheel} onKeyDown={onKey}>
            <img src={image.url} alt="" draggable={false} style={style} />
            <span className="ac-mask" />
          </div>
          <label className="ac-zoom"><Icon name="search" size={15} /><input type="range" min="1" max="4" step="0.01" value={zoom} aria-label={L('Zoom')}
            onChange={(e) => { const z = Number(e.target.value); setZoom(z); setOffset((o) => clamp(o.x, o.y, base * z)); }} /></label>
          <div className="ac-actions">
            <button type="button" className="btn s" disabled={busy} onClick={() => setImage(null)}>{L('Cancel')}</button>
            <button type="button" className="btn s" disabled={busy} onClick={() => input.current?.click()}>{L('Choose another')}</button>
            <button type="button" className="btn" disabled={busy} onClick={save}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save picture')}</button>
          </div>
        </> : <button type="button" className={`ac-drop${over ? ' over' : ''}`} onClick={() => input.current?.click()}
          onDragOver={(e) => { e.preventDefault(); setOver(true); }} onDragLeave={() => setOver(false)}
          onDrop={(e) => { e.preventDefault(); setOver(false); choose(e.dataTransfer.files?.[0]); }}>
          <Icon name="camera" size={28} />
          <b>{L('Choose a picture')}</b>
          <span className="mu">{L('or drop it here · JPG, PNG, GIF or WebP, up to 10 MB')}</span>
        </button>}
        <input ref={input} type="file" accept="image/*" hidden onChange={(e) => { choose(e.target.files?.[0]); e.target.value = ''; }} />
      </div>
    </div>
    {removing && <ConfirmDialog title={L('Remove your picture?')} okText={L('Remove')} okClass="d" busyText={L('Removing…')} onClose={() => setRemoving(false)}
      onConfirm={async () => { await callService('Profile', 'DeleteProfilePicture', {}); notifySuccess(L('Your picture is removed.')); profileChanged(); }}>
      <p style={{ margin: 0 }}>{L('Your initials are shown instead.')}</p>
    </ConfirmDialog>}
  </div>;
}

/* ---------- Password ---------- */

function PasswordTab() {
  const [rules, setRules] = useState(null);
  const [values, setValues] = useState({ current: '', next: '', confirm: '' });
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  useEffect(() => { callService('Profile', 'GetPasswordComplexitySetting', {}).then((r) => setRules(r.setting || {})).catch(() => setRules({})); }, []);

  const p = values.next;
  const checks = useMemo(() => {
    if (!rules) return [];
    const list = [[L('At least {0} characters', rules.minLength || 1), p.length >= (rules.minLength || 1)]];
    if (rules.useLowerCaseLetters) list.push([L('A lowercase letter'), /[a-z]/.test(p)]);
    if (rules.useUpperCaseLetters) list.push([L('An uppercase letter'), /[A-Z]/.test(p)]);
    if (rules.useNumbers) list.push([L('A number'), /[0-9]/.test(p)]);
    if (rules.usePunctuations) list.push([L('A symbol such as ! ? # or %'), /[^A-Za-z0-9]/.test(p)]);
    if (rules.maxLength) list.push([L('At most {0} characters', rules.maxLength), p.length <= rules.maxLength]);
    return list;
  }, [rules, p]);
  const strength = Math.min(4, (p.length >= 8) + (p.length >= 12) + (/[a-z]/.test(p) && /[A-Z]/.test(p)) + /[0-9]/.test(p) + /[^A-Za-z0-9]/.test(p));
  const ok = checks.every(([, pass]) => pass) && p && p === values.confirm && values.current;
  const set = (key) => (e) => setValues((v) => ({ ...v, [key]: e.target.value }));

  async function save(event) {
    event.preventDefault();
    if (!ok) return;
    setBusy(true);
    try {
      await callService('Profile', 'ChangePassword', { currentPassword: values.current, newPassword: values.next });
      notifySuccess(L('Your password is changed. Use it the next time you sign in.'));
      setValues({ current: '', next: '', confirm: '' });
    } catch (_) { /* shown by abp.ajax (e.g. wrong current password) */ } finally { setBusy(false); }
  }

  return <form className="card ac-card" onSubmit={save} autoComplete="on">
    <h3>{L('Change password')}</h3>
    <p className="mu ac-lead">{L('Enter your current password, then the new one twice.')}</p>
    <div className="ac-pw">
      <div className="fm" style={{ gridTemplateColumns: '1fr' }}>
        <label><span>{L('Current password')}<span className="req"> *</span></span>
          <input type={show ? 'text' : 'password'} autoComplete="current-password" required value={values.current} onChange={set('current')} /></label>
        <label><span>{L('New password')}<span className="req"> *</span></span>
          <input type={show ? 'text' : 'password'} autoComplete="new-password" required value={values.next} onChange={set('next')} /></label>
        <div className={`ac-meter s${p ? strength : 0}`} aria-hidden="true"><i /><i /><i /><i /></div>
        <span className="mu ac-meter-text">{p ? L(['Very weak', 'Weak', 'Fair', 'Good', 'Strong'][strength]) : '\u00a0'}</span>
        <label><span>{L('Repeat the new password')}<span className="req"> *</span></span>
          <input type={show ? 'text' : 'password'} autoComplete="new-password" required value={values.confirm} onChange={set('confirm')} /></label>
        {values.confirm && values.confirm !== p && <span className="warn">{L('The two new passwords do not match.')}</span>}
        <label className="ck"><input type="checkbox" checked={show} onChange={(e) => setShow(e.target.checked)} />{L('Show passwords')}</label>
      </div>
      <div className="ac-rules">
        <b>{L('Your new password needs')}</b>
        {rules === null ? <Skeleton kpis={false} lines={3} /> : <ul>{checks.map(([text, pass]) => <li key={text} className={pass ? 'ok' : ''}><span aria-hidden="true">{pass ? '✓' : '○'}</span>{text}</li>)}</ul>}
      </div>
    </div>
    <div className="ac-actions">
      <button type="submit" className="btn" disabled={!ok || busy}>{busy ? <><i className="spn" /> {L('Saving…')}</> : <><Icon name="lock" size={15} />{L('Change password')}</>}</button>
    </div>
  </form>;
}

/* ---------- Sign-in history ---------- */

// AbpLoginResultType names as people read them.
const RESULTS = {
  Success: 'Signed in', InvalidUserNameOrEmailAddress: 'Unknown user name or email', InvalidPassword: 'Wrong password',
  UserIsNotActive: 'Account is inactive', InvalidTenancyName: 'Unknown store', TenantIsNotActive: 'Store is inactive',
  UserEmailIsNotConfirmed: 'Email not confirmed', UnknownExternalLogin: 'Unknown external sign-in', LockedOut: 'Account locked',
  UserPhoneNumberIsNotConfirmed: 'Mobile number not confirmed', FailedForOtherReason: 'Sign-in failed'
};

const PLATFORMS = { WinNT: 'Windows', Win32: 'Windows', Win64: 'Windows', MacOSX: 'macOS', Mac: 'macOS', Linux: 'Linux', Android: 'Android', iPhone: 'iOS', iPad: 'iOS', Unix: 'Unix' };

/** "Chrome on Windows" from what ABP stored: "Chrome / 120.0 / WinNT", or a raw user-agent string (best effort). */
function browserOf(ua) {
  const s = String(ua || '').trim();
  if (!s) return L('Unknown browser');
  const parts = s.split(' / ');
  if (parts.length === 3 && !s.includes('Mozilla/')) {
    const [name, , platform] = parts;
    const browser = name === 'Unknown' ? L('App or script') : name === 'InternetExplorer' ? 'Internet Explorer' : name;
    const os = PLATFORMS[platform] || (platform && platform !== 'Unknown' ? platform : '');
    return os ? L('{0} on {1}', browser, os) : browser;
  }
  const browser = /Edg\//.test(s) ? 'Edge' : /OPR\/|Opera/.test(s) ? 'Opera' : /Firefox\//.test(s) ? 'Firefox' : /Chrome\//.test(s) ? 'Chrome' : /Safari\//.test(s) ? 'Safari' : /node|undici|curl|PostmanRuntime/i.test(s) ? L('App or script') : L('Browser');
  const os = /Windows/.test(s) ? 'Windows' : /Android/.test(s) ? 'Android' : /iPhone|iPad|iOS/.test(s) ? 'iOS' : /Mac OS X/.test(s) ? 'macOS' : /Linux/.test(s) ? 'Linux' : '';
  return os ? L('{0} on {1}', browser, os) : browser;
}

const when = (value) => {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return ['', ''];
  const pad = (x) => String(x).padStart(2, '0');
  return [`${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`, `${pad(d.getHours())}:${pad(d.getMinutes())}`];
};

function SignInsTab() {
  const [rows, setRows] = useState(null);
  useEffect(() => { callService('UserLogin', 'GetRecentUserLoginAttempts', {}).then((r) => setRows(r.items || [])).catch(() => setRows(false)); }, []);
  if (rows === null) return <div className="card"><Skeleton kpis={false} lines={6} /></div>;
  if (rows === false) return <Failed />;
  const failed = rows.filter((r) => r.result !== 'Success').length;
  const lastOk = rows.find((r) => r.result === 'Success');
  return <div className="card ac-card">
    <h3>{L('Sign-in history')}</h3>
    <p className="mu ac-lead">{L('Your last {0} sign-ins. If you do not recognise one, change your password.', rows.length)}</p>
    <div className="ac-sum">
      <div><span className="mu">{L('Last successful sign-in')}</span><b>{lastOk ? when(lastOk.creationTime).join(' ') : '—'}</b></div>
      <div><span className="mu">{L('Failed attempts in this list')}</span><b className={failed ? 'hs-exp' : ''}>{failed}</b></div>
    </div>
    {rows.length ? <ul className="ac-log">{rows.map((r, i) => {
      const [date, time] = when(r.creationTime);
      const ok = r.result === 'Success';
      return <li key={i}>
        <span className={`ac-dot ${ok ? 'ok' : 'bad'}`} aria-hidden="true" />
        <div className="ac-log-main">
          <b>{L(RESULTS[r.result] || r.result)}</b>
          <span className="mu">{browserOf(r.browserInfo)}{r.clientIpAddress ? ` · ${r.clientIpAddress}` : ''}{r.tenancyName ? ` · ${r.tenancyName}` : ''}</span>
        </div>
        <div className="ac-log-when"><b>{date}</b><span className="mu">{time}</span></div>
      </li>;
    })}</ul> : <div className="empty"><b>{L('No sign-ins yet')}</b></div>}
  </div>;
}

function Failed() {
  return <div className="card"><div className="empty"><b>{L('Something went wrong')}</b>{L('Reload the page to try again.')}</div></div>;
}
