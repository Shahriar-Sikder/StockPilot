import { isCostHidden } from './cost-visibility.js';

/**
 * Shows its children unless "Hide cost and values" is on; then shows `fallback` (default nothing).
 * For drawer fields, totals and summary boxes; grid columns use the `cost: true` flag instead.
 * `when={false}` marks the content as not a cost (e.g. a sale price shown by the same markup as a purchase cost).
 */
export default function IfCost({ when = true, fallback = null, children }) {
  return when && isCostHidden() ? fallback : children;
}
