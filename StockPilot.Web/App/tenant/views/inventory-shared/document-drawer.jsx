import { useEffect, useState } from 'react';
import { callService, isGranted, L, notifyError, notifySuccess } from '../abp.js';
import { saveBase64 } from '../api.js';
import { notifyAction } from '../toast.js';
import Avatar, { avatarColour } from './avatar.jsx';
import { ConfirmDialog } from './confirm.jsx';
import { DOCUMENT_TYPES, KINDS, readDocuments } from './document-editor.jsx';
import Drawer, { DrawerHeader } from './drawer.jsx';
import { date, fileSize, n } from './format.js';
import Icon from './icons.jsx';
import IfCost from './if-cost.jsx';
import { FEATURES, isFeatureEnabled } from './features.js';
import Modal from './modal.jsx';
import Skeleton from './skeleton.jsx';
import { DocumentBadge } from './status-badge.jsx';

/** Item picture by item id (document lines carry no PictureId); GetPicture returns null when there is none. */
const linePicture = (itemId) => ({ service: 'ItemMaster', id: itemId, pictureId: `line-${itemId}` });

const NOTES = {
  0: 'Draft: nothing has changed in stock yet.',
  1: 'Posted: stock was updated. Cancelling adds reversing entries.',
  2: 'Cancelled: reversing entries were written to stock history.'
};

/**
 * The mockup's viewDoc(): header, party and currency, the lines with their stock effect, the attached documents
 * (purchases only, PIDOCS), the status note, the total, and Post to stock / Cancel & reverse behind askPost / askCancel.
 */
export default function DocumentDrawer({ kind, id, onClose, onChanged }) {
  const K = KINDS[kind];
  const [doc, setDoc] = useState(null);
  const [reload, setReload] = useState(0);
  const [confirm, setConfirm] = useState(null); // 'post' | 'cancel'
  const [viewing, setViewing] = useState(null); // { name, contentType, base64 }
  // Purchases only, and only while the store has invoice documents (App.Inventory.Purchases.Attachments).
  const showAttachments = !!K.attachments && isFeatureEnabled(FEATURES.purchaseAttachments);
  const canAttach = showAttachments && isGranted(K.attachPermission);

  useEffect(() => {
    let live = true;
    callService(K.service, 'Get', { id }).then((result) => { if (live) setDoc(result); }).catch(onClose);
    return () => { live = false; };
  }, [id, reload]);

  if (!doc) return <Drawer onClose={onClose}><DrawerHeader avatar={<div className="av lg sk" />} title={<span className="sk" style={{ display: 'inline-block', width: 140, height: 18 }} />} onClose={onClose} /><div className="db"><Skeleton /></div></Drawer>;

  const number = K.numberOf(doc);
  // askPost lists the stock change, askCancel the reversal (the opposite sign).
  const summary = (sign) => doc.lines.map((l) => `${sign}${n(l.baseQuantity, 4)} ${l.baseUnitCode} ${l.itemName}`);
  const opposite = K.sign === '+' ? '−' : '+';

  async function addDocuments(event) {
    const files = await readDocuments(event.target.files);
    event.target.value = '';
    if (!files.length) return;
    try {
      for (const file of files) await callService(K.service, 'UploadDocument', { purchaseInvoiceId: doc.id, fileName: file.name, base64Content: file.base64 });
      notifySuccess(L('Document added to {0}', number));
    } catch (_) { /* shown by abp.ajax */ }
    setReload((x) => x + 1);
    onChanged();
  }

  // The mockup's docRm(): remove at once, with Undo in the toast (RestoreDocument; the server keeps the file).
  async function removeDocument(attachment) {
    const refresh = () => { setReload((x) => x + 1); onChanged(); };
    try {
      await callService(K.service, 'DeleteDocument', { id: attachment.id });
      refresh();
      notifyAction('info', L('Document removed'), L('Undo'), async () => {
        await callService(K.service, 'RestoreDocument', { id: attachment.id });
        refresh();
      });
    } catch (_) { /* shown by abp.ajax */ }
  }

  async function open(attachment) {
    try {
      const content = await callService(K.service, 'DownloadDocument', { id: attachment.id });
      if (/^image\//.test(content.contentType)) setViewing({ name: content.fileName, contentType: content.contentType, base64: content.base64Content });
      else saveBase64(content.fileName, content.contentType, content.base64Content);
    } catch (error) { if (!error.handled) notifyError(error.message); }
  }

  return <>
    <Drawer onClose={onClose}>
      <DrawerHeader avatar={<div className="av lg" style={{ background: avatarColour(number) }}>{K.icon}</div>}
        title={number} subtitle={`${L(K.docLabel)} · ${date(K.dateOf(doc))}`} badge={<DocumentBadge status={doc.status} />} onClose={onClose} />
      <div className="db">
        <div className="dl"><div><span>{L(K.partyLabel)}</span>{K.partyOf(doc)}</div><div><span>{L('Currency')}</span>{doc.currencyCode}</div></div>
        {doc.lines.map((l) => <div className="li" key={l.id}>
          <div className="nm"><Avatar name={l.itemName} picture={linePicture(l.itemMasterId)} /><div><b>{l.itemName}</b><span>{n(l.quantity)} {l.unitCode}<IfCost> × {n(K.linePrice(l))}</IfCost></span></div></div>
          <div style={{ textAlign: 'right' }}><IfCost><b>{n(l.lineTotal)}</b></IfCost><div className="mu">{L('{0} stock', `${K.sign}${n(l.baseQuantity, 4)} ${l.baseUnitCode}`)}</div></div>
        </div>)}
        {showAttachments && <>
          <h3 style={{ margin: '16px 0 6px' }}>{L('Documents')}</h3>
          {(doc.attachments || []).length ? doc.attachments.map((a) => <div className="li" key={a.id}>
            <div className="nm"><div className="av" style={{ background: 'var(--ac2)', color: 'var(--ac)' }}><Icon name="file" size={15} /></div>
              <div><b>{a.fileName}</b><span>{fileSize(a.sizeBytes)} · {date(a.creationTime)}</span></div></div>
            <div className="ac">
              <button className="btn s" onClick={() => open(a)}>{L(/^image\//.test(a.contentType) ? 'View' : 'Download')}</button>
              {canAttach && <button className="lnk" onClick={() => removeDocument(a)}>{L('Remove')}</button>}
            </div>
          </div>) : <p className="mu">{L('No documents attached yet.')}</p>}
          {canAttach && <label className="btn s" style={{ marginTop: 8 }}><Icon name="up" size={15} />{L('Add document')}<input type="file" multiple hidden accept={DOCUMENT_TYPES} onChange={addDocuments} /></label>}
        </>}
        <p className="mu" style={{ marginTop: 16 }}>{L(NOTES[doc.status] || NOTES[0])}</p>
      </div>
      <div className="df">
        <IfCost fallback={<span />}><b style={{ fontSize: 17 }}>{doc.currencyCode} {n(doc.totalAmount)}</b></IfCost>
        <div className="ac">
          {doc.status === 0 && isGranted(K.postPermission) && <button className="btn g" onClick={() => setConfirm('post')}>{L('Post to stock')}</button>}
          {doc.status === 1 && isGranted(K.cancelPermission) && <button className="btn d" onClick={() => setConfirm('cancel')}>{L('Cancel & reverse')}</button>}
        </div>
      </div>
    </Drawer>

    {confirm === 'post' && <ConfirmDialog title={L('Post {0}?', number)} okText={L('Post to stock')} okClass="g" busyText={L('Posting…')} onClose={() => setConfirm(null)}
      onConfirm={async () => { await callService(K.service, 'Post', { id: doc.id }); notifySuccess(L('{0} posted. Stock levels are updated.', number)); onChanged(); onClose(); }}>
      <p style={{ marginTop: 0 }}>{L('This updates stock immediately:')}</p>
      <p><b>{summary(K.sign).map((line, i) => <span key={i}>{i > 0 && <br />}{line}</span>)}</b></p>
      <p className="mu">{L('You can cancel it later; that writes reversing entries.')}</p>
    </ConfirmDialog>}
    {confirm === 'cancel' && <ConfirmDialog title={L('Cancel {0}?', number)} okText={L('Cancel document')} okClass="d" busyText={L('Cancelling…')} onClose={() => setConfirm(null)}
      onConfirm={async () => { await callService(K.service, 'Cancel', { id: doc.id }); notifySuccess(L('{0} cancelled. Reversing entries written.', number)); onChanged(); onClose(); }}>
      <p style={{ marginTop: 0 }}>{L('These stock changes will be reversed:')}</p>
      <p><b>{summary(opposite).map((line, i) => <span key={i}>{i > 0 && <br />}{line}</span>)}</b></p>
    </ConfirmDialog>}
    {viewing && <Modal title={viewing.name} size="wd" onClose={() => setViewing(null)} footer={<>
      <button className="btn s" onClick={() => setViewing(null)}>{L('Cancel')}</button>
      <button className="btn" onClick={() => saveBase64(viewing.name, viewing.contentType, viewing.base64)}>{L('Download')}</button>
    </>}><img src={`data:${viewing.contentType};base64,${viewing.base64}`} alt="" style={{ maxWidth: '100%', display: 'block', margin: 'auto' }} /></Modal>}
  </>;
}
