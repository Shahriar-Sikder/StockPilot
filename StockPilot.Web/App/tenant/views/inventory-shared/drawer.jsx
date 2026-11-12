import { useEffect } from 'react';
import { createPortal } from 'react-dom';

/**
 * The mockup's drawer(): dimmed overlay .ov and a 560px panel .dw sliding in from the right.
 * Children are usually <DrawerHeader>, <div className="db">…</div> and <div className="df">…</div>.
 * Escape or a click on the overlay closes it.
 */
export default function Drawer({ onClose, children }) {
  useEffect(() => {
    // A dialog opened over the drawer (Post to stock?) takes Escape first.
    const key = (event) => { if (event.key === 'Escape' && !document.querySelector('.mo')) onClose(); };
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, [onClose]);
  return createPortal(<>
    <div className="ov" onClick={onClose} />
    <div className="dw" role="dialog" aria-modal="true">{children}</div>
  </>, document.body);
}

/** .dh: avatar, title + subtitle, optional badge, close button. */
export function DrawerHeader({ avatar, title, subtitle, badge, onClose }) {
  return <div className="dh">
    {avatar || <span />}
    <div><h2>{title}</h2>{subtitle && <div className="mu">{subtitle}</div>}</div>
    {badge}
    <button type="button" className="x" aria-label="Close" onClick={onClose}>×</button>
  </div>;
}
