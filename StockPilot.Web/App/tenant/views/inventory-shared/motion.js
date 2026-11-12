// Click effect for every .btn and top-bar .chip (styles in motion.css): marks the pressed element with data-clk and
// puts the ripple's start at the pointer (--rx / --ry), or in the middle for the keyboard. An attribute, not a
// class, so React re-rendering the button's class does not cut it. Installed once by main.jsx.
const TARGETS = '.btn, .chip';

function play(el, x, y) {
  if (el.disabled || el.getAttribute('aria-disabled') === 'true') return;
  if (x === undefined) { el.style.removeProperty('--rx'); el.style.removeProperty('--ry'); } else {
    const box = el.getBoundingClientRect();
    el.style.setProperty('--rx', `${Math.round(x - box.left)}px`);
    el.style.setProperty('--ry', `${Math.round(y - box.top)}px`);
  }
  el.removeAttribute('data-clk');
  void el.offsetWidth; // restart the animation on a quick second press
  el.setAttribute('data-clk', '');
}

export function installButtonMotion() {
  if (window.__spButtonMotion) return;
  window.__spButtonMotion = true;
  document.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    const el = event.target.closest && event.target.closest(TARGETS);
    if (el) play(el, event.clientX, event.clientY);
  }, true);
  document.addEventListener('keydown', (event) => {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    const el = document.activeElement && document.activeElement.closest && document.activeElement.closest(TARGETS);
    if (el && !event.repeat) play(el);
  }, true);
  document.addEventListener('animationend', (event) => {
    if (event.animationName === 'mi-ripple' && event.target.matches && event.target.matches(TARGETS)) event.target.removeAttribute('data-clk');
  }, true);
}
