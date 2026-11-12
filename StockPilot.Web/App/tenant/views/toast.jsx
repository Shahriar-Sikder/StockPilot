import { useEffect, useRef, useState } from 'react';
import { takeQueuedToast } from './toast.js';

/**
 * The mockup's #toast: one message at a time, bottom centre, slides up; red (.bad) for errors.
 * A newer toast replaces the current one. Toasts with an action (Undo) stay 6s, others 2.8s.
 * Plain JSX (not an HtmlView template) so the same element stays mounted and the CSS transition runs.
 */
export default function ToastHost() {
  const [toast, setToast] = useState(null);
  const [visible, setVisible] = useState(false);
  const current = useRef(null);
  const timer = useRef(0);

  useEffect(() => {
    function show(detail) {
      clearTimeout(timer.current);
      current.current = detail;
      setToast(detail);
      setVisible(true);
      timer.current = setTimeout(() => setVisible(false), detail.duration || 2800);
    }
    const queued = takeQueuedToast();
    if (queued?.message) show({ id: `queued-${Date.now()}`, type: queued.type || 'success', message: queued.message, duration: 2800 });
    const add = (event) => show(event.detail);
    const dismiss = (event) => { if (!event.detail?.id || event.detail.id === current.current?.id) setVisible(false); };
    window.addEventListener('stockpilot:toast', add);
    window.addEventListener('stockpilot:dismiss-toast', dismiss);
    return () => { clearTimeout(timer.current); window.removeEventListener('stockpilot:toast', add); window.removeEventListener('stockpilot:dismiss-toast', dismiss); };
  }, []);

  function runAction() {
    clearTimeout(timer.current);
    setVisible(false);
    Promise.resolve(toast?.action?.()).catch(() => {});
  }

  const className = `${visible ? 'on' : ''}${toast?.type === 'error' ? ' bad' : ''}`;
  return <div id="toast" className={className} role="status" aria-live="polite">
    {toast?.message}
    {toast?.action && <button type="button" onClick={runAction}>{toast.actionLabel || 'Undo'}</button>}
  </div>;
}
