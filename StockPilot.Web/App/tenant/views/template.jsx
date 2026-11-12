import { useCallback, useState } from 'react';
import { createPortal } from 'react-dom';

/** Escapes text for use inside an HTML template. */
export const escapeHtml = (value) => String(value ?? '').replace(/[&<>"']/g, (ch) => ({
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#39;'
})[ch]);

/** Replaces {{name}} placeholders with values (already escaped by the caller). */
export function fillTemplate(template, values = {}) {
  return template.replace(/\{\{([a-zA-Z0-9_]+)\}\}/g, (match, name) => values[name] ?? '');
}

/**
 * Renders an HTML template. React children are portalled into the element marked data-react-slot, if any.
 * Events bubble from the template markup to the handlers given here.
 */
export function HtmlView({ template, values, className, children, onClick, onChange, onSubmit, onKeyDown }) {
  const [slot, setSlot] = useState(null);
  const html = fillTemplate(template, values);
  const attach = useCallback((node) => {
    setSlot(node ? node.querySelector('[data-react-slot]') : null);
  }, [html]);
  return <>
    <div ref={attach} className={className} dangerouslySetInnerHTML={{ __html: html }} onClick={onClick} onChange={onChange} onSubmit={onSubmit} onKeyDown={onKeyDown} />
    {children && slot ? createPortal(children, slot) : null}
  </>;
}
