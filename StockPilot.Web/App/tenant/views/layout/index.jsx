import React, { useState } from 'react';
import { HtmlView } from '../template.jsx';
import template from './index.html?raw';

/** The public pages' shell: header with a collapsible menu, the page in the slot, footer. */
export default function PublicLayout({ children }) {
  const [menuOpen, setMenuOpen] = useState(false);

  function click(event) {
    const action = event.target.closest('[data-action]')?.dataset.action;
    if (action === 'toggle-menu') setMenuOpen((open) => !open);
    if (action === 'close-menu') setMenuOpen(false);
  }

  return <HtmlView template={template} values={{ navClass: `public-nav${menuOpen ? ' open' : ''}`, year: new Date().getFullYear() }} onClick={click}>
    {children}
  </HtmlView>;
}
