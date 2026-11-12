import { useEffect, useState } from 'react';
import Icon from './icons.jsx';
import { n } from './format.js';

const reducedMotion = () => window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** The mockup's cu(): counts from 0 to value over 800ms with a cubic ease-out (no animation with reduced motion). */
export function CountUp({ value, prefix = '', decimals = 0 }) {
  const target = Number(value) || 0;
  const [shown, setShown] = useState(reducedMotion() ? target : 0);
  useEffect(() => {
    if (reducedMotion()) { setShown(target); return undefined; }
    const start = performance.now();
    let frame = 0;
    const step = (now) => {
      const k = Math.min(1, (now - start) / 800);
      setShown(target * (1 - Math.pow(1 - k, 3)));
      if (k < 1) frame = requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frame);
  }, [target]);
  return <>{prefix}{n(shown, decimals)}</>;
}

/**
 * One KPI card of the mockup's .kp grid: corner icon (KI), label, big number, caption. Lifts 2px on hover (CSS).
 *   <div className="kp"><Kpi icon="coin" colour="var(--ac)" background="var(--ac2)" label="Stock value" value={233416} prefix="৳ " small="6 items tracked" /></div>
 * countUp={false} shows the value as is (the mockup's "Needs attention" card).
 */
export default function Kpi({ icon, colour, background, label, value, prefix = '', small, valueClass, valueStyle, countUp = true }) {
  return <div>
    <i className="ki" style={{ color: colour, background }}><Icon name={icon} size={16} /></i>
    <span>{label}</span>
    <b className={valueClass} style={valueStyle}>{countUp ? <CountUp value={value} prefix={prefix} /> : `${prefix}${value}`}</b>
    {small && <small>{small}</small>}
  </div>;
}
