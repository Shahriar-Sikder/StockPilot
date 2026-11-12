import { useEffect } from 'react';
import { L } from '../abp.js';
import { escapeHtml, HtmlView } from '../template.jsx';
import template from './index.html?raw';

/** First-sign-in dialog (the mockup's welcome()): offers the tour. Escape or a click outside closes it. */
export default function WelcomeDialog({ onClose, onStart }) {
  useEffect(() => {
    const key = (event) => { if (event.key === 'Escape') onClose(); };
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  function click(event) {
    const target = event.target.closest('[data-action]');
    const action = target?.dataset.action;
    if (action === 'cancel' || (action === 'backdrop' && event.target === target)) onClose();
    if (action === 'start') onStart();
  }
  const values = {
    title: escapeHtml(L('Welcome to StockPilot')),
    question: escapeHtml(L('Would you like a one-minute tour of how to run the system?')),
    cancel: escapeHtml(L('Cancel')),
    start: escapeHtml(L('Start the tour'))
  };
  return <HtmlView template={template} values={values} onClick={click} />;
}
