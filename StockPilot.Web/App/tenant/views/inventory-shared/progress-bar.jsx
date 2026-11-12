import { useEffect, useState } from 'react';
import { onPendingRequests } from '../abp.js';

/** The mockup's #pb: a thin bar across the top while any app service request (or abp.ui.setBusy()) is pending. */
export default function ProgressBar() {
  const [active, setActive] = useState(false);
  useEffect(() => onPendingRequests((count) => setActive(count > 0)), []);
  return <div id="pb" className={active ? 'on' : ''} />;
}
