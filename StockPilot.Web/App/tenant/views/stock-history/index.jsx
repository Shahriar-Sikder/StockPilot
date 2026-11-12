import { callService, L } from '../abp.js';
import AbpGrid from '../inventory-shared/abp-grid.jsx';
import { date, n } from '../inventory-shared/format.js';
import { MOVEMENT_TYPES, movementTypeText } from '../inventory-shared/status-badge.jsx';

/**
 * The mockup's moves(): the stock movement log (StockMovement.GetAll, newest first, Excel export on the server),
 * filtered by date range, item and type. The log is read-only.
 */
export default function StockHistoryPage() {
  return <>
    <div className="hd"><div><h1>{L('Stock history')}</h1><p className="sub">{L('Every stock in and out, with the balance after each change. This log is never edited.')}<span className="tag">StockMovement</span></p></div></div>
    <AbpGrid service="StockMovement" exportMethod="GetAllToExcel" name="Stock history"
      filters={[
        { key: 'from', param: 'dateFrom', type: 'date', label: 'From' },
        { key: 'to', param: 'dateTo', type: 'date', label: 'To' },
        { key: 'item', param: 'itemMasterId', all: 'All items', number: true, load: () => callService('ItemMaster', 'GetForCombo').then((r) => r.items.map((i) => [i.id, i.itemName])) },
        { key: 'type', param: 'movementType', all: 'All types', number: true, options: MOVEMENT_TYPES.map((type) => [type, movementTypeText(type)]) }
      ]}
      columns={[
        { key: 'movementDate', label: 'Date', text: (r) => date(r.movementDate) },
        { key: 'itemName', label: 'Item' },
        { key: 'movementType', label: 'What happened', text: (r) => movementTypeText(r.movementType) },
        { key: 'quantity', label: 'Quantity', right: true, text: (r) => r.quantity, render: (r) => <span className={r.quantity > 0 ? 'pos' : 'neg'}>{r.quantity > 0 ? '+' : ''}{n(r.quantity, 2)}</span> },
        { key: 'baseUnitCode', label: 'Unit', nosort: true },
        { key: 'unitCost', label: 'Unit cost', right: true, cost: true },
        { key: 'balanceAfter', label: 'Balance after', right: true },
        { key: 'referenceNo', label: 'Reference' }
      ]} />
  </>;
}
