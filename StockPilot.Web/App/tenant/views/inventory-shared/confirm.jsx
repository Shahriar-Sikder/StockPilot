import { useEffect, useState } from 'react';
import Modal from './modal.jsx';
import { L } from '../abp.js';

// Dialogs behind abp.message.* (wired in abp.js installUi): server errors from abp.ajax, and confirm() calls.
let push = null;
const waiting = [];

function enqueue(dialog) {
  return new Promise((resolve) => {
    const entry = { ...dialog, resolve };
    if (push) push(entry); else waiting.push(entry);
  });
}

/** abp.message.info/success/warn/error. Resolves when closed. */
export const openAlert = ({ type = 'info', message, title }) => enqueue({ kind: 'alert', type, message, title });

/** abp.message.confirm. Resolves true / false. */
export const openConfirm = ({ message, title, okText, danger }) => enqueue({ kind: 'confirm', message, title, okText, danger });

/**
 * A page-level confirmation in the mockup's style (askPost / askCancel / logoutAsk): body content, Cancel and
 * an action button that shows a spinner and busyText ("Posting…") while onConfirm runs, then closes.
 * If onConfirm rejects (abp.ajax has already shown the server's message), the dialog stays open.
 */
export function ConfirmDialog({ title, children, okText, okClass = '', okDisabled = false, busyText, onConfirm, onClose, size = '' }) {
  const [busy, setBusyState] = useState(false);
  async function confirmAction() {
    setBusyState(true);
    try {
      await onConfirm();
      onClose();
    } catch (_) {
      setBusyState(false);
    }
  }
  return <Modal title={title} size={size} onClose={busy ? undefined : onClose} footer={<>
    <button type="button" className="btn s" disabled={busy} onClick={onClose}>{L('Cancel')}</button>
    <button type="button" className={`btn ${okClass}`.trim()} disabled={busy || okDisabled} onClick={confirmAction}>{busy ? <><i className="spn" /> {busyText || okText}</> : okText}</button>
  </>}>{children}</Modal>;
}

const titles = { error: 'Something went wrong', warn: 'Please check', success: 'Done', info: 'Information' };

/** Renders one dialog at a time, in order. Mounted once by the shell. */
export default function ConfirmHost() {
  const [queue, setQueue] = useState([]);
  useEffect(() => {
    push = (entry) => setQueue((current) => [...current, entry]);
    if (waiting.length) setQueue((current) => [...current, ...waiting.splice(0)]);
    return () => { push = null; };
  }, []);

  const dialog = queue[0];
  if (!dialog) return null;
  const close = (value) => { dialog.resolve(value); setQueue((current) => current.slice(1)); };
  const confirmMode = dialog.kind === 'confirm';
  const title = dialog.title || (confirmMode ? L('Are you sure?') : L(titles[dialog.type] || titles.info));

  return <Modal title={title} onClose={() => close(false)} footer={confirmMode
    ? <><button type="button" className="btn s" onClick={() => close(false)}>{L('Cancel')}</button><button type="button" className={`btn ${dialog.danger ? 'd' : ''}`} onClick={() => close(true)} autoFocus>{dialog.okText || L('OK')}</button></>
    : <button type="button" className="btn" onClick={() => close(true)} autoFocus>{L('OK')}</button>}>
    <p style={{ margin: 0, whiteSpace: 'pre-line' }} className={dialog.type === 'error' ? '' : undefined}>{dialog.message}</p>
  </Modal>;
}
