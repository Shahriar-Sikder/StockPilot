// Toast API used across the app (and by abp.notify.* through abp.js installUi).
// The mockup shows one toast at a time at the bottom centre: 2.8s, or 6s when it has an Undo action.
let nextId = 1;

const TOAST_MS = 2800;
const ACTION_MS = 6000;
const allowed = new Set(['success', 'error', 'warning', 'warn', 'info']);

export function notify(type, message, duration = TOAST_MS) {
  const toast = { id: String(nextId++), type: allowed.has(type) ? type : 'info', message: String(message || '') };
  window.dispatchEvent(new CustomEvent('stockpilot:toast', { detail: { ...toast, duration } }));
  return toast.id;
}

/** A toast with one action button (the mockup's "Undo"). */
export function notifyAction(type, message, actionLabel, action, duration = ACTION_MS) {
  const toast = { id: String(nextId++), type: allowed.has(type) ? type : 'info', message: String(message || ''), actionLabel: String(actionLabel || 'Undo'), action };
  window.dispatchEvent(new CustomEvent('stockpilot:toast', { detail: { ...toast, duration } }));
  return toast.id;
}

export function dismissToast(id) {
  window.dispatchEvent(new CustomEvent('stockpilot:dismiss-toast', { detail: { id } }));
}

/** A toast to show after the next full page load (e.g. "Signed in" across the post-login reload). */
export function queueToast(type, message) {
  try { sessionStorage.setItem('stockpilot.pendingToast', JSON.stringify({ type, message })); } catch (_) { /* session storage may be disabled */ }
}

export function takeQueuedToast() {
  try {
    const value = sessionStorage.getItem('stockpilot.pendingToast');
    sessionStorage.removeItem('stockpilot.pendingToast');
    return value ? JSON.parse(value) : null;
  } catch (_) { return null; }
}

export function setFormLoading(form, loading, label = 'Please wait…') {
  const button = form?.querySelector('button[type="submit"]');
  if (!button) return;
  if (loading) {
    button.dataset.originalLabel = button.innerHTML;
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    button.innerHTML = `<i class="spn" aria-hidden="true"></i> ${label}`;
  } else {
    button.disabled = false;
    button.removeAttribute('aria-busy');
    button.innerHTML = button.dataset.originalLabel || button.innerHTML;
    delete button.dataset.originalLabel;
  }
}
