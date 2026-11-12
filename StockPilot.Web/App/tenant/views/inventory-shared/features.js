import { getFeatureValues, isFeatureEnabled as abpIsFeatureEnabled } from '../abp.js';

/**
 * Store features (StockPilot.Core\Features\AppFeatures.cs), read from abp.features (loaded with the page, so a change
 * by the host applies on the next page load). The server enforces them; these only hide what the store cannot use.
 */
export const FEATURES = {
  purchases: 'App.Inventory.Purchases',
  purchaseAttachments: 'App.Inventory.Purchases.Attachments',
  purchaseExcelImport: 'App.Inventory.Purchases.ExcelImport',
  sales: 'App.Inventory.Sales',
  reports: 'App.Inventory.Reports',
  export: 'App.Inventory.Export',
  multiCurrency: 'App.Inventory.MultiCurrency',
  unitConversions: 'App.Inventory.UnitConversions',
  pictures: 'App.Inventory.Pictures',
  hideCostAndValues: 'App.HideCostAndValues',
  maxUserCount: 'App.MaxUserCount',
  maxItemCount: 'App.MaxItemCount'
};

// Parent of a child feature (a child only counts while its parent is on).
const PARENTS = {
  [FEATURES.purchaseAttachments]: FEATURES.purchases,
  [FEATURES.purchaseExcelImport]: FEATURES.purchases
};

/**
 * True when the store has the feature, and its parent for a child feature. A name the server does not send is
 * treated as on (only defined features are sent; this keeps pages working if the scripts failed to load features).
 */
export function isFeatureEnabled(name) {
  if (!name) return true;
  const values = getFeatureValues();
  if (!(name in values)) return true;
  if (!abpIsFeatureEnabled(name)) return false;
  return PARENTS[name] ? isFeatureEnabled(PARENTS[name]) : true;
}

/** The raw value (e.g. '20' for a limit), or undefined. */
export function getFeatureValue(name) {
  return getFeatureValues()[name];
}
