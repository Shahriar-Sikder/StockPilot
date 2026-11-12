import { useEffect, useRef, useState } from 'react';
import { currentLanguage, L } from '../abp.js';

/** The reCAPTCHA field name the server reads (Recaptcha.Web), sent without the model. prefix. */
export const CAPTCHA_FIELD = 'g-recaptcha-response';

let loading = null;

/** Google's reCAPTCHA v2 script, loaded once, rendered explicitly. */
function loadRecaptcha() {
  if (window.grecaptcha && window.grecaptcha.render) return Promise.resolve(window.grecaptcha);
  if (loading) return loading;
  loading = new Promise((resolve, reject) => {
    window.spRecaptchaLoaded = () => resolve(window.grecaptcha);
    const script = document.createElement('script');
    script.src = `https://www.google.com/recaptcha/api.js?onload=spRecaptchaLoaded&render=explicit&hl=${encodeURIComponent(currentLanguage() || 'en')}`;
    script.async = true;
    script.onerror = () => { loading = null; script.remove(); reject(new Error('recaptcha')); };
    document.head.appendChild(script);
  });
  return loading;
}

/**
 * The security check on the registration form, shown when the server asks for one (RegistrationOptions.useCaptcha).
 * Rendered inside the form, so Google's hidden g-recaptcha-response field goes with the form data.
 * onReady(widgetId) lets the page reset it after a failed submit (an answer can be used only once).
 */
export default function Captcha({ siteKey, onReady }) {
  const box = useRef(null);
  const [state, setState] = useState(siteKey ? 'loading' : 'missing');

  useEffect(() => {
    if (!siteKey) return undefined;
    let live = true;
    loadRecaptcha().then((grecaptcha) => {
      if (!live || !box.current || box.current.childElementCount) return;
      const dark = ['dark', 'black'].includes(document.documentElement.dataset.theme);
      const id = grecaptcha.render(box.current, { sitekey: siteKey, theme: dark ? 'dark' : 'light' });
      setState('ready');
      onReady(id);
    }).catch(() => { if (live) setState('failed'); });
    return () => { live = false; };
  }, [siteKey]);

  return <div className="public-field public-captcha">
    <label>{L('Security check')}</label>
    <div ref={box} />
    {state === 'loading' && <small>{L('Loading the security check…')}</small>}
    {state === 'failed' && <div className="public-form-message error" role="alert">{L('The security check did not load. Check your connection and reload the page.')}</div>}
    {state === 'missing' && <div className="public-form-message error" role="alert">{L('Registration needs a security check that is not set up yet. Please try again later.')}</div>}
  </div>;
}
