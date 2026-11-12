import { useEffect, useState } from 'react';
import { callService, L } from '../abp.js';
import { date, day, n } from '../inventory-shared/format.js';
import './host.css';

// Pieces the host pages share: status badges, the edition list, the features tree and usage bars.

export const go = (route) => { window.location.hash = `#${route}`; };

/* ---------- store requests ---------- */

// TenantRequestStatus: 0 Pending, 1 Approved, 2 Rejected, 3 Cancelled.
export const REQUEST_STATUS = { 0: ['Draft', 'Pending'], 1: ['OK', 'Approved'], 2: ['Cancelled', 'Rejected'], 3: ['hc', 'Cancelled'] };

export function RequestBadge({ status }) {
  const [className, label] = REQUEST_STATUS[status] || REQUEST_STATUS[0];
  return <span className={`b ${className}`}>{L(label)}</span>;
}

/* ---------- subscriptions ---------- */

// TenantSubscriptionStatus: 0 None, 1 Active, 2 ExpiringSoon, 3 Expired.
export const SUBSCRIPTION = { None: 0, Active: 1, ExpiringSoon: 2, Expired: 3 };

/** The end date in red once it has passed, amber while it ends soon, "No end date" when there is none. */
export function SubscriptionDate({ value, status }) {
  if (!value) return <span className="mu">{L('No end date')}</span>;
  const className = status === SUBSCRIPTION.Expired ? 'hs-exp' : status === SUBSCRIPTION.ExpiringSoon ? 'hs-soon' : '';
  return <span className={className} title={status === SUBSCRIPTION.Expired ? L('Expired') : status === SUBSCRIPTION.ExpiringSoon ? L('Ends soon') : undefined}>{date(value)}</span>;
}

/** Days from today to a server date (negative once passed). */
export function daysLeft(value) {
  if (!value) return null;
  return Math.round((new Date(date(value)) - new Date(day(0))) / 864e5);
}

export function daysLeftText(value) {
  const days = daysLeft(value);
  if (days === null) return '';
  if (days < 0) return L('Ended {0} days ago', -days);
  if (days === 0) return L('Ends today');
  return days === 1 ? L('Ends tomorrow') : L('Ends in {0} days', days);
}

/** Server date input: yyyy-mm-dd, or null when empty. */
export const dateInput = (value) => (value ? `${value}T00:00:00` : null);

/* ---------- editions ---------- */

/** [[id, name]] of all editions ('' = no edition), loaded once per page. */
export function useEditions() {
  const [editions, setEditions] = useState([]);
  useEffect(() => {
    callService('Edition', 'GetEditionComboboxItems', 0)
      .then((items) => setEditions((items || []).filter((item) => item.value && item.value !== 'null').map((item) => [item.value, item.displayText])))
      .catch(() => { /* shown by abp.ajax */ });
  }, []);
  return editions;
}

/* ---------- features ---------- */

const isOn = (value) => String(value).toLowerCase() === 'true';
const same = (a, b) => String(a ?? '').toLowerCase() === String(b ?? '').toLowerCase();

/**
 * Edition.GetEditionForEdit's FlatFeatureDto list (IInputType objects) as the tree's rows, in parent-first order,
 * with the edition's values (featureValues [{name, value}]).
 */
export function editionRows(features = [], featureValues = []) {
  const values = Object.fromEntries(featureValues.map((v) => [v.name, v.value]));
  const byName = Object.fromEntries(features.map((f) => [f.name, f]));
  const depthOf = (f) => { let d = 0, p = f.parentName; while (p && byName[p] && d < 10) { d++; p = byName[p].parentName; } return d; };
  const rows = features.map((f) => {
    const input = f.inputType || {};
    const validator = input.validator || {};
    const kind = /CHECKBOX/i.test(input.name || '') ? 'checkbox' : /NUMERIC/i.test(validator.name || '') || validator.minValue !== undefined ? 'number' : 'text';
    return {
      name: f.name, displayName: f.displayName, description: f.description, parentName: f.parentName, depth: depthOf(f), inputType: kind,
      minValue: validator.minValue, maxValue: validator.maxValue, defaultValue: f.defaultValue, value: values[f.name] ?? f.defaultValue
    };
  });
  return orderTree(rows);
}

/** Tenant.GetTenantFeaturesForEdit's rows: the edition value is the default, the store's effective value the value. */
export function tenantRows(features = []) {
  return orderTree(features.map((f) => ({ ...f, defaultValue: f.editionValue, value: f.effectiveValue })));
}

function orderTree(rows) {
  const children = (parent) => rows.filter((r) => (r.parentName || null) === parent);
  const out = [];
  const walk = (parent) => children(parent).forEach((row) => { out.push(row); walk(row.name); });
  walk(null);
  rows.forEach((row) => { if (!out.includes(row)) out.push(row); }); // a parent that is not listed
  return out;
}

/** {name: value} of the rows. */
export const rowValues = (rows) => Object.fromEntries(rows.map((r) => [r.name, r.value]));

/** [{name, value}] of the values that differ from the rows' defaults (approval's FeatureOverrides). */
export const differences = (rows, values) => rows.filter((r) => !same(values[r.name], r.defaultValue)).map((r) => ({ name: r.name, value: String(values[r.name]) }));

/** Usage of a limit feature: [used, label] (App.MaxUserCount, App.MaxItemCount). */
export function usageOf(name, usage) {
  if (!usage) return null;
  if (name === 'App.MaxUserCount') return [usage.userCount, usage.userCount === 1 ? L('1 user now') : L('{0} users now', usage.userCount)];
  if (name === 'App.MaxItemCount') return [usage.activeItemCount, usage.activeItemCount === 1 ? L('1 active item now') : L('{0} active items now', usage.activeItemCount)];
  return null;
}

/**
 * The features tree: a switch or number field per feature, children indented and disabled while their parent is
 * off. defaultLabel names where the default comes from ("Edition default"); with overrides, a value that differs
 * from the default shows an Overridden badge and a "Use edition default" link. 0 for a limit means no limit.
 */
export function FeatureTree({ rows, values, onChange, defaultLabel = 'Edition default', overrides = false, usage, readOnly = false }) {
  const byName = Object.fromEntries(rows.map((r) => [r.name, r]));
  const parentOff = (row) => { let p = row.parentName; while (p && byName[p]) { if (byName[p].inputType === 'checkbox' && !isOn(values[p])) return true; p = byName[p].parentName; } return false; };
  const shown = (row, value) => (row.inputType === 'checkbox' ? L(isOn(value) ? 'On' : 'Off') : row.inputType === 'number' && String(value) === '0' ? L('No limit') : value);

  return <div className="ft-tree">{rows.map((row) => {
    const value = values[row.name] ?? '';
    const disabled = readOnly || parentOff(row);
    const overridden = overrides && !same(value, row.defaultValue);
    const used = usageOf(row.name, usage);
    const limit = Number(value);
    const share = used && limit > 0 ? used[0] / limit : 0;
    return <div key={row.name} className={`ft-row${disabled && !readOnly ? ' off' : ''}`} style={{ paddingLeft: 4 + row.depth * 22 }}>
      <div className="ft-main">
        <b>{row.displayName || row.name}{overridden && <span className="b Draft ft-badge">{L('Overridden')}</span>}</b>
        {row.description && row.description !== row.displayName && <span className="mu">{row.description}</span>}
        <span className="mu">{L(defaultLabel)}: {shown(row, row.defaultValue)}
          {overridden && !disabled && <> · <button type="button" className="lnk b2" onClick={() => onChange(row.name, row.defaultValue)}>{L('Use edition default')}</button></>}
        </span>
        {used && <span className={`mu${share >= 1 ? ' hs-exp' : share >= 0.8 ? ' hs-soon' : ''}`}>{used[1]}{limit > 0 ? ` · ${L('{0}% used', n(share * 100, 0))}` : ''}</span>}
      </div>
      <div className="ft-input">
        {row.inputType === 'checkbox'
          ? <label className="ft-switch"><input type="checkbox" disabled={disabled} checked={isOn(value)} aria-label={row.displayName} onChange={(event) => onChange(row.name, event.target.checked ? 'true' : 'false')} /><i /></label>
          : <input type={row.inputType === 'number' ? 'number' : 'text'} disabled={disabled} value={value} min={row.minValue ?? undefined} max={row.maxValue ?? undefined} aria-label={row.displayName} onChange={(event) => onChange(row.name, event.target.value)} />}
      </div>
    </div>;
  })}</div>;
}

/** A usage bar: green, amber from 80%, red at the limit. limit 0 = no limit. */
export function UsageBar({ label, used, limit }) {
  const share = limit > 0 ? used / limit : 0;
  const className = share >= 1 ? 'e' : share >= 0.8 ? 'w' : '';
  return <div className="hs-usage">
    <div className="hs-usage-top"><b>{label}</b><span className="mu">{limit > 0 ? L('{0} of {1}', n(used, 0), n(limit, 0)) : L('{0} · no limit', n(used, 0))}</span></div>
    <div className={`bar ${className}`.trim()} style={{ width: '100%' }}><i style={{ width: `${Math.min(100, Math.round(share * 100))}%` }} /></div>
  </div>;
}

/** Pills used as tabs inside a drawer. */
export function Tabs({ tabs, value, onChange }) {
  return <div className="pills" role="tablist">{tabs.map(([key, label]) => <button type="button" role="tab" aria-selected={value === key} key={key} className={value === key ? 'on' : ''} onClick={() => onChange(key)}>{L(label)}</button>)}</div>;
}

/** An app-service error as text (abp.ajax has usually shown it already). */
export const errorText = (error) => error?.message || error?.error?.message || L('Something went wrong');
