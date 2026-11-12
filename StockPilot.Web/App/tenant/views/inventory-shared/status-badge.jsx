import { L } from '../abp.js';

// The mockup's .b badges. Server enums arrive as numbers; class names are the mockup's (Draft / Posted /
// Cancelled / OK / Low / Out). Each *Text helper gives the plain text for grid search columns and exports.

const DOCUMENT = { 0: ['Draft', 'Draft'], 1: ['Posted', 'Posted'], 2: ['Cancelled', 'Cancelled'] };
const STOCK = { 0: ['OK', 'In stock'], 1: ['Low', 'Low stock'], 2: ['Out', 'Out of stock'], 3: [null, 'Not stocked'] };
const IMPORT = { 0: ['OK', 'Ready'], 1: ['Draft', 'Exists'], 2: ['Out', 'Error'] };

export const documentStatusText = (status) => L((DOCUMENT[status] || DOCUMENT[0])[1]);
export const stockStatusText = (status) => L((STOCK[status] || STOCK[0])[1]);
export const importStatusText = (status) => L((IMPORT[status] || IMPORT[0])[1]);

// StockMovementType, worded as the mockup's TY table (the adjustment and transfer types are not in the mockup).
const MOVEMENT = { 1: 'Opening stock', 2: 'Received', 3: 'Returned to supplier', 4: 'Sold', 5: 'Customer return', 6: 'Issued', 7: 'Adjustment in', 8: 'Adjustment out', 9: 'Transfer in', 10: 'Transfer out' };
export const movementTypeText = (type) => L(MOVEMENT[type] || String(type));
export const MOVEMENT_TYPES = Object.keys(MOVEMENT).map(Number);

/** DocumentStatus: 0 Draft, 1 Posted, 2 Cancelled. */
export function DocumentBadge({ status }) {
  const [className, text] = DOCUMENT[status] || DOCUMENT[0];
  return <span className={`b ${className}`}>{L(text)}</span>;
}

/** ItemStockStatus: 0 Ok, 1 Low, 2 Out, 3 NotStocked (a neutral chip; the mockup has no service items). */
export function StockBadge({ status }) {
  const [className, text] = STOCK[status] || STOCK[0];
  return className ? <span className={`b ${className}`}>{L(text)}</span> : <span className="ch">{L(text)}</span>;
}

/** PurchaseImportStatus: 0 Ready, 1 Exists, 2 Error (colours as the mockup's import preview). */
export function ImportBadge({ status }) {
  const [className, text] = IMPORT[status] || IMPORT[0];
  return <span className={`b ${className}`}>{L(text)}</span>;
}
