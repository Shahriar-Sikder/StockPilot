import { useState } from 'react';
import { L } from '../abp.js';
import { ListPage } from '../generic-crud/index.jsx';
import { LISTS, setupTabs } from '../generic-crud/pages.js';

const TAB_KEY = 'stockpilot.setup.tab';
const readTab = () => { const tabs = setupTabs(); try { const tab = sessionStorage.getItem(TAB_KEY); return tabs.includes(tab) ? tab : tabs[0]; } catch (_) { return tabs[0]; } };

/** The mockup's setup(): heading, one pill per reference list, then that list (embedded listPage). */
export default function SetupPage() {
  const [tab, setTab] = useState(readTab);
  const choose = (key) => { setTab(key); try { sessionStorage.setItem(TAB_KEY, key); } catch (_) { /* private mode */ } };
  return <>
    <div className="hd"><div><h1>{L('Setup')}</h1><p className="sub">{L('The reference lists that items, suppliers and documents use.')}</p></div></div>
    <div className="pills">{setupTabs().map((key) => <button key={key} className={tab === key ? 'on' : ''} onClick={() => choose(key)}>{L(LISTS[key].title)}</button>)}</div>
    {tab ? <ListPage key={tab} list={tab} embedded /> : <div className="card"><div className="empty"><b>{L('No setup lists for you')}</b>{L('Ask an administrator for access to the lists you need.')}</div></div>}
  </>;
}
