import { useEffect, useState } from 'react';
import { callService, isGranted, L, notifySuccess } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { ConfirmDialog } from '../inventory-shared/confirm.jsx';
import { date } from '../inventory-shared/format.js';
import Icon from '../inventory-shared/icons.jsx';
import Modal from '../inventory-shared/modal.jsx';
import Skeleton from '../inventory-shared/skeleton.jsx';
import { editionRows, FeatureTree, rowValues } from '../host-shared/index.jsx';

/** Editions: the feature sets stores start from. List, add, edit (name and features tree) and delete. */
export default function HostEditionsPage() {
  const [rows, setRows] = useState(null);
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState(null); // {} = new, {id} = edit
  const [deleting, setDeleting] = useState(null);

  useEffect(() => { callService('Edition', 'GetEditions', {}).then((r) => setRows(r.items || [])).catch(() => setRows([])); }, [reload]);
  const changed = () => setReload((k) => k + 1);
  const canEdit = isGranted('Pages.Editions.Edit');
  const canDelete = isGranted('Pages.Editions.Delete');

  const columns = [
    { key: 'displayName', label: 'Edition', render: (e) => <div className="nm"><div><b>{e.displayName}</b><span>{e.name}</span></div></div> },
    { key: 'creationTime', label: 'Created', text: (e) => date(e.creationTime) },
    ...(canEdit || canDelete ? [{ key: 'actions', label: '', nosort: true, stop: true, right: true, render: (e) => <>
      {canEdit && <button className="lnk b2" onClick={() => setEditing({ id: e.id })}><Icon name="edit" size={14} />{L('Edit')}</button>}
      {canDelete && <button className="lnk" onClick={() => setDeleting(e)}><Icon name="trash" size={14} />{L('Delete')}</button>}
    </> }] : [])
  ];

  return <>
    <div className="hd">
      <div><h1>{L('Editions')}</h1><p className="sub">{L('Feature sets for stores. A store follows its edition unless you change a feature for that store.')}</p></div>
      {isGranted('Pages.Editions.Create') && <div className="ac"><button className="btn l" onClick={() => setEditing({})}><Icon name="plus" size={17} />{L('Add edition')}</button></div>}
    </div>
    <AbpGrid rows={rows} columns={columns} onRowClick={canEdit ? (e) => setEditing({ id: e.id }) : undefined} />
    {editing && <EditionModal id={editing.id} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); notifySuccess(L('Edition saved. Stores see the change on their next page load.')); changed(); }} />}
    {deleting && <ConfirmDialog title={L('Delete {0}?', deleting.displayName)} okText={L('Delete')} okClass="d" busyText={L('Deleting…')} onClose={() => setDeleting(null)}
      onConfirm={async () => { await callService('Edition', 'DeleteEdition', { id: deleting.id }); notifySuccess(L('Edition deleted.')); changed(); }}>
      <p style={{ margin: 0 }}>{L('Stores on this edition keep their own feature values only and use the defaults for the rest.')}</p>
    </ConfirmDialog>}
  </>;
}

function EditionModal({ id, onClose, onSaved }) {
  const [name, setName] = useState('');
  const [rows, setRows] = useState(null);
  const [values, setValues] = useState({});
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    callService('Edition', 'GetEditionForEdit', { id: id ?? null }).then((output) => {
      const list = editionRows(output.features, output.featureValues);
      setName(output.edition?.displayName || '');
      setRows(list);
      setValues(rowValues(list));
    }).catch(() => onClose());
  }, [id]);

  async function save(event) {
    event.preventDefault();
    setBusy(true);
    try {
      await callService('Edition', 'CreateOrUpdateEdition', {
        edition: { id: id ?? null, displayName: name.trim() },
        featureValues: rows.map((r) => ({ name: r.name, value: String(values[r.name] ?? '') }))
      });
      onSaved();
    } catch (_) { setBusy(false); }
  }

  return <Modal title={id ? L('Edit edition') : L('Add edition')} size="wd" onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="submit" form="edition-form" className="btn" disabled={busy || !rows}>{busy ? <><i className="spn" /> {L('Saving…')}</> : L('Save')}</button>
  </>}>
    {!rows ? <Skeleton kpis={false} lines={6} /> : <form id="edition-form" onSubmit={save}>
      <label className="fl"><span>{L('Name')}<span className="req"> *</span></span><input required maxLength={64} value={name} onChange={(event) => setName(event.target.value)} /></label>
      <div className="hs-section">{L('Features')}</div>
      <FeatureTree rows={rows} values={values} defaultLabel="Default" onChange={(feature, value) => setValues((v) => ({ ...v, [feature]: value }))} />
      <p className="hs-note">{L('A limit of 0 means no limit.')}</p>
    </form>}
  </Modal>;
}
