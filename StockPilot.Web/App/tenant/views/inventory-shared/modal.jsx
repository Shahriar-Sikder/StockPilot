import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';

/**
 * The mockup's modal() / shell(): overlay .mo with a .md card (header .mh, body .mb, optional footer .mf).
 * Escape and a click on the overlay close it; the first input or select gets focus.
 * size: '' | 'wd' (640px) | 'xl' (900px, scrolling body).
 * header replaces the plain <b>{title}</b>; actions go next to the × inside an .ac group (the editor's
 * "Import from Excel"); footerStyle styles the .mf (the editor puts its total on the left).
 */
export default function Modal({ title, header, actions, children, footer, footerStyle, onClose, size = '', className = '' }) {
  const overlay = useRef(null);
  const close = useRef(onClose);
  close.current = onClose;
  useEffect(() => {
    const field = overlay.current && overlay.current.querySelector('.mb input:not([type=file]):not([hidden]), .mb select, .mb textarea');
    if (field) field.focus();
    const key = (event) => {
      // Only the top-most dialog reacts (an alert opened over a dialog closes first).
      const all = document.querySelectorAll('.mo');
      if (event.key === 'Escape' && close.current && all[all.length - 1] === overlay.current) close.current();
    };
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  const x = onClose && <button type="button" className="x" aria-label="Close" onClick={onClose}>×</button>;
  return createPortal(
    <div className="mo" ref={overlay} onMouseDown={(event) => { if (event.target === overlay.current && onClose) onClose(); }}>
      <div className={`md ${size} ${className}`.trim()} role="dialog" aria-modal="true">
        <div className="mh">{header || <b>{title}</b>}{actions ? <div className="ac">{actions}{x}</div> : x}</div>
        <div className="mb">{children}</div>
        {footer && <div className="mf" style={footerStyle}>{footer}</div>}
      </div>
    </div>,
    document.body
  );
}
