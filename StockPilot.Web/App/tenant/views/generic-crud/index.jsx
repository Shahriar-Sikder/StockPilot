import { useEffect, useMemo, useRef, useState } from 'react';
import { callService, clearBusy, isGranted, L, notifyError, notifySuccess, setBusy } from '../abp.js';
import { notifyAction } from '../toast.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { usePicture } from '../inventory-shared/avatar.jsx';
import Icon from '../inventory-shared/icons.jsx';
import Modal from '../inventory-shared/modal.jsx';
import { date, day, n } from '../inventory-shared/format.js';
import { useHashQuery } from '../inventory-shared/hash-query.js';
import { CODE_LABEL, LISTS, withPictureFeature } from './pages.js';

/**
 * The mockup's listPage() + fields() + openAdd/openEdit/del: one reference list (setup tabs, Suppliers) on its
 * ABP app service. Paging, sorting, search and the dropdown filters run on the server (AbpGrid); Delete is a
 * soft delete with Undo (Restore). Export Excel is the service's GetAllToExcel (same filters and sorting on the server).
 */

/* ---------- combos (GetForCombo) for foreign keys, shared by the grid filters, cells and the form ---------- */

// Each option is [id, label, comboRow]; the row is kept for dependent selects (filterBy) and beforeSave.
export function useCombos(config) {
  const services = useMemo(() => [...new Set([...config.fields, ...(config.filters || [])].filter((f) => f.fk).map((f) => f.fk))], [config]);
  const loaders = useRef({});
  const [combos, setCombos] = useState({});
  const load = (service) => {
    if (!loaders.current[service]) {
      loaders.current[service] = callService(service, 'GetForCombo')
        .then((result) => (result.items || []).map((row) => [row.id, CODE_LABEL.has(service) ? row.code : row.name, row]));
      loaders.current[service].then((list) => setCombos((current) => ({ ...current, [service]: list }))).catch(() => { /* shown by abp.ajax */ });
    }
    return loaders.current[service];
  };
  useEffect(() => { services.forEach(load); }, [services]);
  // After a save in this list, combos of the same service (e.g. units listed in unit conversions) may be stale.
  const refresh = () => { loaders.current = {}; services.forEach(load); };
  return { combos, load, refresh };
}

const comboLabel = (combos, service, id) => {
  const hit = (combos[service] || []).find(([value]) => value === id);
  return hit ? hit[1] : null;
};

/* ---------- grid cells ---------- */

function PictureCell({ service, row }) {
  const src = usePicture(service, row.id, row.pictureId);
  return src ? <img className="th" src={src} alt="" /> : <span className="mu">—</span>;
}

function cellValue(field, row, combos) {
  const value = row[field.key];
  if (field.type === 'fk') return comboLabel(combos, field.fk, value) ?? row[field.col] ?? '-';
  if (field.type === 'bool') return value ? L('Yes') : L('No');
  if (field.type === 'date') return date(value);
  if (field.type === 'img') return value ? L('Yes') : L('No');
  return value;
}

function columnsFor(config, combos, actions) {
  const columns = config.fields.map((field) => ({
    key: field.key,
    label: field.label,
    sortKey: field.sortKey,
    nosort: field.type === 'img',
    right: field.type === 'num',
    text: (row) => cellValue(field, row, combos),
    render: field.type === 'img' ? (row) => <PictureCell service={config.service} row={row} />
      : field.type === 'num' ? (row) => n(row[field.key]) : undefined
  }));
  if (actions) columns.push({ key: '_a', label: '', nosort: true, noexport: true, stop: true, render: actions });
  return columns;
}

/* ---------- add / edit dialog (the mockup's fields() inside modal()) ---------- */

// Resized in the browser to at most 220px like the mockup's imgPick(); the server resizes again and stores PNG.
function readPicture(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error(L('The picture could not be read.')));
    reader.onload = () => {
      const image = new Image();
      image.onerror = () => reject(new Error(L('The picture could not be read.')));
      image.onload = () => {
        const scale = Math.min(1, 220 / Math.max(image.width, image.height));
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, Math.round(image.width * scale));
        canvas.height = Math.max(1, Math.round(image.height * scale));
        canvas.getContext('2d').drawImage(image, 0, 0, canvas.width, canvas.height);
        resolve(canvas.toDataURL('image/jpeg', 0.82));
      };
      image.src = reader.result;
    };
    reader.readAsDataURL(file);
  });
}

function emptyRecord(config) {
  const record = {};
  config.fields.forEach((field) => {
    if (field.type === 'bool') record[field.key] = false;
    else if (field.type === 'date') record[field.key] = day(0);
    else if (field.type !== 'img' && field.type !== 'fk') record[field.key] = field.default ?? '';
  });
  return record;
}

/**
 * Add (row null) or edit (row = { id, pictureId? }) one record of `config`.
 * field.filterBy = { key, prop }: only options whose combo row[prop] equals this form's `key` value
 * (sub-categories of the chosen category). config.beforeSave(input, combos) can fill derived fields.
 */
export function RecordDialog({ config: listConfig, row, combos, onClose, onSaved }) {
  // Without the Pictures feature the form has no picture field.
  const config = withPictureFeature(listConfig);
  const editing = !!row;
  const [model, setModel] = useState(editing ? null : emptyRecord(config));
  const existingPicture = usePicture(config.picture && editing ? config.service : null, row && row.id, row && row.pictureId);
  const [picture, setPicture] = useState(undefined); // undefined: unchanged, '': removed, data URL: new picture
  const saveButton = useRef(null);
  const fileInput = useRef(null);

  useEffect(() => {
    if (!editing) return;
    callService(config.service, 'GetForEdit', { id: row.id })
      .then((result) => setModel({ ...result, effectiveDate: result.effectiveDate ? String(result.effectiveDate).slice(0, 10) : result.effectiveDate }))
      .catch(onClose);
  }, []);

  const options = (field) => {
    const all = combos[field.fk] || [];
    if (!field.filterBy) return all;
    const parent = Number(value(config.fields.find((f) => f.key === field.filterBy.key)));
    return all.filter((option) => option[2] && option[2][field.filterBy.prop] === parent);
  };
  // Foreign keys default to the first option, as the mockup's selects do (also when a parent change
  // leaves a dependent select's value outside its options).
  function value(field) {
    const current = model ? model[field.key] : '';
    if (field.type === 'fk') {
      const list = options(field);
      const empty = current === undefined || current === null || current === '';
      if (empty || (field.filterBy && !list.some(([id]) => id === current))) return list[0]?.[0] ?? '';
    }
    return current ?? '';
  }
  const update = (key, next) => setModel((current) => ({ ...current, [key]: next }));

  async function pick(event) {
    const file = event.target.files && event.target.files[0];
    if (!file) return;
    try { setPicture(await readPicture(file)); } catch (error) { notifyError(error.message); }
  }

  async function save() {
    const input = { ...model };
    for (const field of config.fields) {
      if (field.type === 'img') continue;
      let next = value(field);
      // An empty optional number is 0 (the mockup's +value); an empty optional select is null.
      if (field.type === 'num') next = next === '' ? (field.required ? null : 0) : Number(next);
      if (field.type === 'fk') next = next === '' ? null : Number(next);
      if (field.required && (next === null || next === '' || (typeof next === 'number' && Number.isNaN(next)))) {
        notifyError(L('Please fill in {0}', L(field.label)));
        return;
      }
      input[field.key] = next;
    }
    if (config.beforeSave) config.beforeSave(input, combos);
    setBusy(saveButton.current);
    try {
      let id = row && row.id;
      if (editing) await callService(config.service, 'Update', input);
      else id = (await callService(config.service, 'Create', input)).id;
      if (config.picture && picture) await callService(config.service, 'UploadPicture', { id, fileName: 'picture.jpg', base64Content: picture.split(',')[1] });
      else if (config.picture && picture === '' && row && row.pictureId) await callService(config.service, 'DeletePicture', { id });
      notifySuccess(L(editing ? config.updated : config.saved));
      onSaved();
      onClose();
    } catch (_) {
      // abp.ajax already showed the server's message; the dialog stays open so nothing typed is lost.
    } finally {
      if (saveButton.current) clearBusy(saveButton.current);
    }
  }

  const shownPicture = picture === undefined ? existingPicture : picture;
  const footer = <>
    <button type="button" className="btn s" onClick={onClose}>{L('Cancel')}</button>
    <button type="button" className="btn" id="mok" ref={saveButton} disabled={!model} onClick={save}>{L(editing ? 'Save changes' : config.save)}</button>
  </>;

  return <Modal title={L(editing ? config.edit : config.add)} size={config.wide ? 'wd' : ''} onClose={onClose} footer={footer}>
    {!model ? <div className="fm">{config.fields.map((field) => <div key={field.key} className="sk" style={{ height: 34 }} />)}</div>
      : <form className="fm" onSubmit={(event) => { event.preventDefault(); save(); }}>
        {config.fields.map((field) => {
          // One <span>, so the label text and the required star stay on one line in the column layout (.fm label).
          const label = <span>{L(field.label)}{field.required && <span className="req"> *</span>}</span>;
          if (field.type === 'img') return <label key={field.key} style={{ gridColumn: '1/-1' }}>{L('Picture (optional)')}
            <input type="file" ref={fileInput} accept="image/*" onChange={pick} />
            <div style={{ marginTop: 6 }}>{shownPicture
              ? <><img className="th lg" src={shownPicture} alt="" /> <button type="button" className="lnk" onClick={() => { setPicture(''); if (fileInput.current) fileInput.current.value = ''; }}>{L('Remove')}</button></>
              : <span className="mu">{L('No picture')}</span>}</div>
          </label>;
          if (field.type === 'bool') return <label key={field.key} className="ck"><input type="checkbox" checked={!!model[field.key]} onChange={(event) => update(field.key, event.target.checked)} />{L(field.label)}</label>;
          if (field.type === 'fk') return <label key={field.key}>{label}<select value={value(field)} onChange={(event) => update(field.key, Number(event.target.value))}>
            {options(field).map(([id, text]) => <option key={id} value={id}>{text}</option>)}
          </select></label>;
          return <label key={field.key}>{label}<input type={field.type === 'num' ? 'number' : field.type === 'date' ? 'date' : 'text'} step={field.type === 'num' ? 'any' : undefined}
            value={value(field)} onChange={(event) => update(field.key, event.target.value)} /></label>;
        })}
        <button type="submit" hidden />
      </form>}
  </Modal>;
}

/* ---------- the list ---------- */

/**
 * embedded: inside Setup (only the right-aligned Add button above the grid);
 * otherwise a page of its own with the mockup's heading ("2 records" + entity tag).
 */
export function ListPage({ list, embedded = false }) {
  const config = withPictureFeature(LISTS[list]);
  const { combos, load, refresh } = useCombos(config);
  const [reload, setReload] = useState(0);
  const [dialog, setDialog] = useState(null); // { row } — row null to add
  const [count, setCount] = useState(null);
  const canCreate = isGranted(config.canCreate), canEdit = isGranted(config.canEdit), canDelete = isGranted(config.canDelete);

  const changed = () => { setReload((x) => x + 1); refresh(); };

  // The page heading counts every live record, whatever the grid is filtered to.
  useEffect(() => {
    if (embedded) return;
    callService(config.service, 'GetAll', { skipCount: 0, maxResultCount: 1 }).then((result) => setCount(result.totalCount)).catch(() => {});
  }, [reload]);

  // #/suppliers?new=1 (the New menu and the palette) opens the add dialog.
  useHashQuery((query) => { if (query.has('new') && canCreate) setDialog({ row: null }); });

  async function remove(row) {
    await callService(config.service, 'Delete', { id: row.id });
    changed();
    notifyAction('info', L('Deleted'), L('Undo'), async () => {
      await callService(config.service, 'Restore', { id: row.id });
      changed();
    });
  }

  const actions = (canEdit || canDelete) ? (row) => <>
    {canEdit && <button className="lnk b2" onClick={() => setDialog({ row })}><Icon name="edit" size={13} />{L('Edit')}</button>}
    {canDelete && <button className="lnk" onClick={() => remove(row).catch(() => {})}><Icon name="trash" size={13} />{L('Delete')}</button>}
  </> : null;

  const filters = (config.filters || []).map((f) => ({ key: f.key, all: f.all, number: true, load: () => load(f.fk) }));
  const addButton = canCreate && <button className="btn" onClick={() => setDialog({ row: null })}><Icon name="plus" size={15} />{L(config.add)}</button>;

  return <>
    {embedded
      ? <div className="ac" style={{ justifyContent: 'flex-end', marginBottom: 14 }}>{addButton}</div>
      : <div className="hd">
        <div><h1>{L(config.title)}</h1><p className="sub">{count === null ? '\u00a0' : L(count === 1 ? '{0} record' : '{0} records', count)}<span className="tag">{config.tag}</span></p></div>
        <div className="ac">{addButton}</div>
      </div>}
    <AbpGrid service={config.service} exportMethod="GetAllToExcel" name={config.title} pdf reloadKey={reload} filters={filters}
      columns={columnsFor(config, combos, actions)} />
    <p className="mu">{L('Deleting only marks a record as deleted, so Undo always works.')}</p>
    {dialog && <RecordDialog config={config} row={dialog.row} combos={combos} onClose={() => setDialog(null)} onSaved={changed} />}
  </>;
}

/** A list with a route of its own (#/suppliers). */
export default function GenericCrudPage({ config }) {
  return <ListPage list={config.list} />;
}
