import { useEffect, useRef, useState } from 'react';
import { callService, clearBusy, L, notifyError, setBusy } from '../abp.js';
import { downloadTempFile, saveBlob } from '../api.js';
import Icon from './icons.jsx';
import { n } from './format.js';
import { withoutCost } from './cost-visibility.js';
import { FEATURES, isFeatureEnabled } from './features.js';
import { getWorkspaceInfo } from './workspace.js';

/**
 * The mockup's grid() (toolbar, filters, sortable headers, sticky header, ~10 rows then scroll, empty state,
 * rows per page, Prev / Next), with paging, sorting, search and filters done on the server.
 *
 * Props
 *   service, method ('GetAll')      app service + method taking a [Entity]GridInput, returning { totalCount, items }
 *   exportMethod                    e.g. 'GetAllToExcel' (same input, no paging) -> FileDto; shows "Export Excel"
 *   pdf, name                       "Export PDF" (client side, jsPDF, all filtered rows) and its title
 *   columns  [{ key, label, sortKey?, nosort?, right?, render?(row), text?(row), noexport?, stop?, cost? }]
 *            cost: a cost or value column, left out everywhere while "Hide cost and values" is on (the server
 *            Excel export leaves it out too)
 *            sortKey: server sort field (default key); text: plain value for PDF (default row[key]);
 *            stop: clicks inside the cell do not open the row (action buttons)
 *   filters  [{ key, param?, type: 'select' | 'date', all?, label?, options?: [[value, label]], load?: () => Promise<[[value, label]]>, number?, map?(value) }]
 *            map: turns the chosen value into what the server takes (e.g. a date into the end of that day)
 *   searchParam     input field the search box fills (default 'filter')
 *   input           extra input merged into every request (pill status, includeDeleted, …); a change resets to page 1
 *   defaultSorting  server sorting when no header is chosen
 *   rowClass(row), onRowClick(row), onLoaded(result), reloadKey (change to reload), pageSize (default 50, as the mockup)
 */
export default function AbpGrid({
  service, method = 'GetAll', exportMethod, pdf = false, name = '', columns: allColumns, filters = [], input: extraInput = {},
  defaultSorting, rowClass, onRowClick, onLoaded, reloadKey = 0, pageSize = 50, searchable = true, searchParam = 'filter', rowKey = 'id',
  rows: localRows, onView
}) {
  // Local mode (rows given): for app services that return a whole, unpaged result (the reports). Search, sort and
  // paging then run over those rows in the browser; onView(rows) receives the searched + sorted rows (Print uses them).
  // exportMethod still runs on the server with `input`.
  // Columns flagged cost: true are left out of the table, sorting, search and PDF while costs are hidden.
  const columns = withoutCost(allColumns);
  const canExport = isFeatureEnabled(FEATURES.export);
  const local = localRows !== undefined; // null while the caller is loading
  const [text, setText] = useState('');
  const [query, setQuery] = useState('');
  const [values, setValues] = useState({});
  const [sort, setSort] = useState(null);
  const [page, setPage] = useState(1);
  const [size, setSize] = useState(pageSize);
  const [data, setData] = useState(null);
  const [options, setOptions] = useState({});
  const request = useRef(0);
  const excelButton = useRef(null);
  const pdfButton = useRef(null);
  const extraKey = JSON.stringify(extraInput);

  // Search: 300ms after typing stops, back to page 1.
  useEffect(() => {
    const timer = setTimeout(() => { setQuery(text.trim()); setPage(1); }, 300);
    return () => clearTimeout(timer);
  }, [text]);

  // Pills and other outside filters also start again at page 1.
  const firstExtra = useRef(true);
  useEffect(() => { if (firstExtra.current) { firstExtra.current = false; return; } setPage(1); }, [extraKey]);

  // Filter dropdowns that load their options from the server (combos), again after each outside reload
  // (a new customer or supplier appears in the filter once a document with it is saved).
  useEffect(() => {
    filters.filter((f) => f.load).forEach((f) => {
      f.load().then((list) => setOptions((current) => ({ ...current, [f.key]: list }))).catch(() => { /* shown by abp.ajax */ });
    });
  }, [reloadKey]);

  function buildInput(paged) {
    const column = sort && columns.find((c) => c.key === sort.key);
    const input = { ...extraInput };
    if (query) input[searchParam] = query;
    const sorting = column ? `${column.sortKey || column.key} ${sort.dir > 0 ? 'asc' : 'desc'}` : defaultSorting;
    if (sorting) input.sorting = sorting;
    filters.forEach((f) => {
      const value = values[f.key];
      if (value === undefined || value === '') return;
      input[f.param || f.key] = f.map ? f.map(value) : f.number ? Number(value) : value;
    });
    if (paged) { input.skipCount = (page - 1) * size; input.maxResultCount = size; }
    return input;
  }

  useEffect(() => {
    if (!local) return;
    if (!localRows) { setData(null); return; }
    const value = (column, row) => { const v = column.text ? column.text(row) : row[column.key]; return v === null || v === undefined ? '' : v; };
    let list = localRows;
    if (query) {
      const q = query.toLowerCase();
      list = list.filter((row) => columns.some((column) => String(value(column, row)).toLowerCase().includes(q)));
    }
    const column = sort && columns.find((c) => c.key === sort.key);
    if (column) {
      list = [...list].sort((a, b) => {
        const x = value(column, a), y = value(column, b);
        return (typeof x === 'number' && typeof y === 'number' ? x - y : String(x).localeCompare(String(y), undefined, { numeric: true })) * sort.dir;
      });
    }
    if (onView) onView(list);
    const pages = Math.max(1, Math.ceil(list.length / size));
    if (page > pages) { setPage(pages); return; }
    setData({ totalCount: list.length, items: list.slice((page - 1) * size, page * size) });
  }, [localRows, query, sort && sort.key, sort && sort.dir, page, size]);

  // A new local result (other report or parameters) starts again at page 1.
  useEffect(() => { if (local) setPage(1); }, [localRows]);

  useEffect(() => {
    if (local) return;
    const id = ++request.current;
    callService(service, method, buildInput(true))
      .then((result) => {
        if (id !== request.current) return;
        // A delete can leave the current page empty: step back.
        const pages = Math.max(1, Math.ceil((result.totalCount || 0) / size));
        if (page > pages) { setPage(pages); return; }
        setData(result);
        if (onLoaded) onLoaded(result);
      })
      .catch(() => { if (id === request.current) setData((current) => current || { items: [], totalCount: 0 }); });
  }, [service, method, query, JSON.stringify(values), sort && sort.key, sort && sort.dir, page, size, extraKey, reloadKey]);

  const setFilter = (key, value) => { setValues((current) => ({ ...current, [key]: value })); setPage(1); };
  const reset = () => { setText(''); setQuery(''); setValues({}); setPage(1); };
  const sortBy = (column) => {
    if (column.nosort) return;
    setSort((current) => (current && current.key === column.key ? { key: column.key, dir: -current.dir } : { key: column.key, dir: 1 }));
  };

  async function exportExcel() {
    setBusy(excelButton.current);
    try {
      const file = await callService(service, exportMethod, buildInput(false));
      await downloadTempFile(file);
    } catch (_) { /* abp.ajax already showed server errors */ } finally { clearBusy(excelButton.current); }
  }

  async function exportPdf() {
    setBusy(pdfButton.current);
    try { await gridPdf({ service, method, input: buildInput(false), columns, name }); }
    catch (error) { if (!error.handled) notifyError(error.message); }
    finally { clearBusy(pdfButton.current); }
  }

  const total = data ? data.totalCount || 0 : 0;
  const pages = Math.max(1, Math.ceil(total / size));
  const start = (page - 1) * size;
  const rows = data ? data.items || [] : [];

  return <div className="card tw">
    <div className="gt">
      {filters.map((f) => f.type === 'date'
        ? <label key={f.key} className="gl">{L(f.label)}<input type="date" value={values[f.key] || ''} onChange={(event) => setFilter(f.key, event.target.value)} /></label>
        : <select key={f.key} className="fs" value={values[f.key] ?? ''} onChange={(event) => setFilter(f.key, event.target.value)}>
          <option value="">{L(f.all)}</option>
          {(f.options || options[f.key] || []).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>)}
      {searchable && <input className="sr" placeholder={L('Search…')} value={text} onChange={(event) => setText(event.target.value)} />}
      <span className="sp" />
      {filters.length > 0 && <button className="btn s" onClick={reset}>{L('Reset')}</button>}
      {exportMethod && canExport && <button className="btn s" ref={excelButton} onClick={exportExcel}><Icon name="dl" size={15} />{L('Export Excel')}</button>}
      {pdf && canExport && <button className="btn s" ref={pdfButton} onClick={exportPdf}><Icon name="print" size={15} />{L('Export PDF')}</button>}
    </div>
    <div>
      {!data && <GridSkeleton />}
      {data && (total ? <div className="tw"><table>
        <thead><tr>{columns.map((column) => <th key={column.key} className={`${column.right ? 'r' : ''} ${column.nosort ? '' : 'srt'}`.trim()} onClick={() => sortBy(column)}>
          {column.label ? L(column.label) : ''}{sort && sort.key === column.key ? (sort.dir > 0 ? ' ▲' : ' ▼') : ''}
        </th>)}</tr></thead>
        <tbody>{rows.map((row, index) => <tr key={row[rowKey] ?? index} className={`${rowClass ? rowClass(row) || '' : ''} ${onRowClick ? 'row' : ''}`.trim()} onClick={onRowClick ? () => onRowClick(row) : undefined}>
          {columns.map((column) => <td key={column.key} className={column.right ? 'r' : undefined} onClick={column.stop ? (event) => event.stopPropagation() : undefined}>
            {column.render ? column.render(row) : cellText(column, row)}
          </td>)}
        </tr>)}</tbody>
      </table></div> : <div className="empty"><b>{L('No records found')}</b>{query ? L('Try a different search.') : L('Nothing here yet.')}</div>)}
      {data && <div className="pgr">
        <span className="mu">{total ? L('{0}–{1} of {2}', start + 1, Math.min(start + size, total), total) : L('0 records')}</span>
        <span className="sp" />
        <label className="mu">{L('Rows')} <select value={size} onChange={(event) => { setSize(Number(event.target.value)); setPage(1); }}>{[10, 25, 50, 100].map((z) => <option key={z} value={z}>{z}</option>)}</select></label>
        <button className="btn s" disabled={page <= 1} onClick={() => setPage(page - 1)}>{L('‹ Prev')}</button>
        <span className="mu">{L('Page {0} of {1}', page, pages)}</span>
        <button className="btn s" disabled={page >= pages} onClick={() => setPage(page + 1)}>{L('Next ›')}</button>
      </div>}
    </div>
  </div>;
}

function cellText(column, row) {
  const value = column.text ? column.text(row) : row[column.key];
  if (value === null || value === undefined) return '';
  return typeof value === 'number' && column.right ? n(value) : String(value);
}

function GridSkeleton() {
  return <div style={{ padding: '4px 12px' }}>{Array.from({ length: 6 }, (_, i) => <div key={i} className="sk" style={{ height: 14, margin: '12px 0' }} />)}</div>;
}

/* ---------- Export PDF (no server PDF export exists, so jsPDF builds it in the browser, as the mockup does) ---------- */

const ACCENTS = { blue: '#2563eb', red: '#dc2626', orange: '#ea580c', yellow: '#ca8a04', green: '#059669', teal: '#0d9488', purple: '#7c3aed', pink: '#db2777' };

function loadScript(src) {
  return new Promise((resolve, reject) => {
    if (document.querySelector(`script[src="${src}"]`)) { resolve(); return; }
    const script = document.createElement('script');
    script.src = src;
    script.onload = resolve;
    script.onerror = () => reject(new Error(L('The PDF library did not load. Check your connection.')));
    document.head.appendChild(script);
  });
}

async function loadJsPdf() {
  if (!(window.jspdf && window.jspdf.jsPDF)) await loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
  const probe = new window.jspdf.jsPDF();
  if (typeof probe.autoTable !== 'function') await loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf-autotable/3.8.2/jspdf.plugin.autotable.min.js');
  return window.jspdf.jsPDF;
}

/** All rows matching the filters, a page of 1000 at a time (at most 10,000). */
async function fetchAll(service, method, input) {
  const all = [];
  let total = Infinity;
  while (all.length < total && all.length < 10000) {
    const result = await callService(service, method, { ...input, skipCount: all.length, maxResultCount: 1000 });
    total = result.totalCount || 0;
    all.push(...(result.items || []));
    if (!(result.items || []).length) break;
  }
  return all;
}

async function gridPdf({ service, method, input, columns, name }) {
  const [JsPdf, rows] = await Promise.all([loadJsPdf(), fetchAll(service, method, input)]);
  const cols = columns.filter((c) => !c.noexport && c.label);
  const doc = new JsPdf({ orientation: cols.length > 6 ? 'landscape' : 'portrait', unit: 'pt', format: 'a4' });
  const pageWidth = doc.internal.pageSize.getWidth(), pageHeight = doc.internal.pageSize.getHeight();
  const accent = ACCENTS[document.documentElement.dataset.accent] || ACCENTS.blue;
  const now = new Date();
  const printed = `${now.toISOString().slice(0, 10)} ${now.toTimeString().slice(0, 5)}`;
  const title = L(name);
  doc.setFontSize(15); doc.text(title, 40, 40);
  doc.setFontSize(9); doc.setTextColor(110);
  doc.text(`${getWorkspaceInfo().tenantName ? `${getWorkspaceInfo().tenantName} · ` : ''}${L('Printed')} ${printed} · ${L(rows.length === 1 ? '{0} record' : '{0} records', rows.length)}`, 40, 56);
  const columnStyles = {};
  cols.forEach((c, i) => { if (c.right) columnStyles[i] = { halign: 'right' }; });
  doc.autoTable({
    startY: 68,
    head: [cols.map((c) => L(c.label))],
    body: rows.map((row) => cols.map((c) => { const value = c.text ? c.text(row) : row[c.key]; return typeof value === 'number' ? n(value) : String(value ?? ''); })),
    styles: { fontSize: 8, cellPadding: 4 },
    headStyles: { fillColor: [1, 3, 5].map((i) => parseInt(accent.substr(i, 2), 16)) },
    columnStyles,
    margin: { left: 40, right: 40 },
    didDrawPage: () => { doc.setFontSize(8); doc.setTextColor(110); doc.text(`${L('Page')} ${doc.internal.getNumberOfPages()}`, pageWidth - 70, pageHeight - 20); }
  });
  saveBlob(`${title.replace(/\W+/g, '_')}.pdf`, doc.output('blob'));
}
