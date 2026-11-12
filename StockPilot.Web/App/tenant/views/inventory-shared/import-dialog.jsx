import { useRef, useState } from 'react';
import { callService, clearBusy, isGranted, L, notifySuccess, setBusy } from '../abp.js';
import { downloadTempFile } from '../api.js';
import { date, n } from './format.js';
import Icon from './icons.jsx';
import { isCostHidden } from './cost-visibility.js';
import Modal from './modal.jsx';
import { ImportBadge, importStatusText } from './status-badge.jsx';

/** First message of an invoice, with "(+N more)" as in the mockup; rows are numbered as in the sheet. */
function messageOf(invoice) {
  const messages = invoice.messages || [];
  if (!messages.length) return invoice.status === 1 ? L('Already in the system, skipped') : '';
  const first = messages[0];
  const text = first.rowNumber ? L('Row {0}: {1}', first.rowNumber, first.message) : first.message;
  return messages.length > 1 ? `${text} ${L('(+{0} more)', messages.length - 1)}` : text;
}

/**
 * The mockup's openImport(): upload an .xlsx or .csv of receipts; the server reads it (PurchaseInvoiceImport.Preview,
 * base64), the dialog lists one row per InvoiceNo with Ready / Exists / Error, then Import creates the Ready ones as
 * drafts or posts them. The template comes from PurchaseInvoiceImport.GetTemplate.
 */
export default function ImportDialog({ onClose, onImported }) {
  const [file, setFile] = useState(null); // { name, base64 }
  const [reading, setReading] = useState(false);
  const [preview, setPreview] = useState(null);
  const [creating, setCreating] = useState(false);
  const templateButton = useRef(null);
  const draftButton = useRef(null);
  const postButton = useRef(null);

  async function choose(event) {
    const picked = event.target.files && event.target.files[0];
    if (!picked) return;
    setReading(true);
    setPreview(null);
    try {
      const base64 = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result).split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(picked);
      });
      const next = { name: picked.name, base64 };
      setFile(next);
      setPreview(await callService('PurchaseInvoiceImport', 'Preview', { fileName: next.name, base64Content: next.base64 }));
    } catch (_) {
      // abp.ajax showed the server's message (unsupported file, unreadable sheet…)
    } finally {
      setReading(false);
    }
  }

  async function template() {
    setBusy(templateButton.current);
    try { await downloadTempFile(await callService('PurchaseInvoiceImport', 'GetTemplate')); } catch (_) { /* shown */ } finally { clearBusy(templateButton.current); }
  }

  async function create(post) {
    const button = post ? postButton.current : draftButton.current;
    setCreating(true);
    setBusy(button, L('Creating…'));
    try {
      const result = await callService('PurchaseInvoiceImport', 'Import', { fileName: file.name, base64Content: file.base64, postAfterImport: post });
      const count = (result.created || []).length;
      notifySuccess(L(post ? (count === 1 ? '{0} invoice created and posted' : '{0} invoices created and posted') : (count === 1 ? '{0} invoice created as drafts' : '{0} invoices created as drafts'), count));
      onImported();
      onClose();
    } catch (_) {
      // shown by abp.ajax
    } finally {
      setCreating(false);
      if (button) clearBusy(button);
    }
  }

  const ready = preview ? preview.readyCount : 0;
  const invoices = preview ? preview.invoices || [] : [];
  const footer = <>
    <button className="btn s" onClick={onClose}>{L('Cancel')}</button>
    <button className="btn" ref={draftButton} disabled={!ready || creating} onClick={() => create(false)}>{L('Create drafts')}</button>
    {isGranted('Pages.StockPilot.Purchases.Post') && <button className="btn g" ref={postButton} disabled={!ready || creating} onClick={() => create(true)}>{L('Create & post')}</button>}
  </>;

  return <Modal size="xl" header={<b>{L('Import receipts from Excel')}</b>} onClose={onClose} footer={footer}>
    <p style={{ marginTop: 0 }} dangerouslySetInnerHTML={{ __html: L('Upload an .xlsx or .csv file. One <b>purchase invoice</b> is created for every distinct <b>InvoiceNo</b> in the file.') }} />
    <div className="card" style={{ padding: 12, marginBottom: 14 }}><span className="mu">{L('Columns:')}</span> InvoiceNo, InvoiceDate, SupplierCode, ItemCode, Unit, Quantity, UnitCost, Currency {L('(optional). Invoice numbers that already exist are skipped.')}</div>
    <div className="ac">
      <input type="file" accept=".xlsx,.csv" onChange={choose} />
      <button className="btn s" ref={templateButton} onClick={template}><Icon name="dl" size={15} />{L('Download template')}</button>
    </div>
    <div style={{ marginTop: 16 }}>
      {reading && <div className="ld"><i className="spn" /> {L('Reading file…')}</div>}
      {preview && <>
        <p><b>{ready}</b> {L(ready === 1 ? 'invoice ready,' : 'invoices ready,')} <b>{invoices.length - ready}</b> {L('skipped or with errors.')}</p>
        {invoices.length ? <div className="card tw"><div className="tw"><table>
          <thead><tr><th>{L('Invoice no')}</th><th>{L('Date')}</th><th>{L('Supplier')}</th><th className="r">{L('Lines')}</th>{!isCostHidden() && <th className="r">{L('Total')}</th>}<th>{L('Result')}</th></tr></thead>
          <tbody>{invoices.map((inv, i) => <tr key={`${inv.invoiceNo}-${i}`}>
            <td>{inv.invoiceNo || L('(no number)')}</td><td>{date(inv.invoiceDate)}</td><td>{inv.supplierName}</td>
            <td className="r">{n(inv.lineCount)}</td>{!isCostHidden() && <td className="r">{n(inv.total)}</td>}
            <td title={importStatusText(inv.status)}><ImportBadge status={inv.status} /> <span className="mu">{messageOf(inv)}</span></td>
          </tr>)}</tbody>
        </table></div></div>
          : <div className="empty"><b>{L('No rows found')}</b>{L('Check that the first row has the column headings.')}</div>}
      </>}
    </div>
  </Modal>;
}
