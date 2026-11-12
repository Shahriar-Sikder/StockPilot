import { L } from '../abp.js';

/**
 * Temporary page for routes whose React page is built in a later phase of the frontend work
 * (dashboard, purchases, sales, stock history, reports). Uses the mockup's heading and empty state.
 */
export default function PendingPage({ title, sub }) {
  return <>
    <div className="hd"><div><h1>{L(title)}</h1>{sub && <p className="sub">{L(sub)}</p>}</div></div>
    <div className="card"><div className="empty"><b>{L('This page is being rebuilt')}</b>{L('It will show live data from the server in a later step.')}</div></div>
  </>;
}
