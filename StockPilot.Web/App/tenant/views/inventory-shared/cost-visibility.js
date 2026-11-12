import { isFeatureEnabled } from '../abp.js';

/** Feature App.HideCostAndValues (StockPilot.Core\Features\AppFeatures.cs). */
export const HIDE_COST_FEATURE = 'App.HideCostAndValues';

/**
 * True when the workspace has "Hide cost and values" on: view pages, prints and exports leave out unit cost,
 * average cost, stock value and cost totals, and the dashboard shows quantities only. Display only; entry forms
 * keep their cost inputs and no calculation changes. abp.features is loaded once with the page, so a change
 * takes effect after a reload.
 */
export function isCostHidden() {
  return isFeatureEnabled(HIDE_COST_FEATURE);
}

/**
 * The `cost: true` flag: entries of a list (grid columns, report totals) that are a cost or a value. While costs are
 * hidden they are left out; otherwise the list is returned as it is. For anything else use <IfCost> (if-cost.jsx).
 */
export function withoutCost(list) {
  return isCostHidden() ? list.filter((entry) => !entry.cost) : list;
}

