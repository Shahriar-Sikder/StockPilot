import React, { useEffect, useRef, useState } from 'react';
import { L } from '../abp.js';
import './landing.css';

/*
 * The public landing page. Everything that moves is drawn in code (CSS, SVG, one small canvas): no GIF or video files.
 * Motion stops for people who ask for less of it (prefers-reduced-motion): the page then shows the final state of
 * every animation, numbers included. Background work (canvas, live numbers) also pauses while off screen or hidden.
 */

const reducedMotion = () => typeof window !== 'undefined' && !!window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const finePointer = () => typeof window !== 'undefined' && !!window.matchMedia && window.matchMedia('(hover: hover) and (pointer: fine)').matches;
const money = (value) => '৳' + Math.round(value).toLocaleString('en-IN');

/** True once the element has scrolled into view (stays true). */
function useInView(ref, threshold = 0.25) {
  const [seen, setSeen] = useState(false);
  useEffect(() => {
    const node = ref.current;
    if (!node || seen) return undefined;
    if (!('IntersectionObserver' in window)) { setSeen(true); return undefined; }
    const observer = new IntersectionObserver(([entry]) => { if (entry.isIntersecting) { setSeen(true); observer.disconnect(); } }, { threshold });
    observer.observe(node);
    return () => observer.disconnect();
  }, [ref, seen, threshold]);
  return seen;
}

/** Counts from 0 to target once run is true (eased); the final value straight away with reduced motion. */
function useCountUp(target, run, duration = 1600) {
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (!run) return undefined;
    if (reducedMotion()) { setValue(target); return undefined; }
    let frame;
    let start;
    const step = (time) => {
      if (start === undefined) start = time;
      const progress = Math.min(1, (time - start) / duration);
      setValue(target * (1 - Math.pow(1 - progress, 3)));
      if (progress < 1) frame = requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frame);
  }, [target, run, duration]);
  return value;
}

/** Calls fn every ms while the page is visible and motion is allowed. */
function useTicker(fn, ms, enabled = true) {
  const saved = useRef(fn);
  saved.current = fn;
  useEffect(() => {
    if (!enabled || reducedMotion()) return undefined;
    const id = setInterval(() => { if (!document.hidden) saved.current(); }, ms);
    return () => clearInterval(id);
  }, [ms, enabled]);
}

/** Adds .in to every [data-reveal] inside root as it scrolls into view. */
function useReveal(rootRef) {
  useEffect(() => {
    const root = rootRef.current;
    if (!root) return undefined;
    const items = root.querySelectorAll('[data-reveal]');
    if (reducedMotion() || !('IntersectionObserver' in window)) { items.forEach((item) => item.classList.add('in')); return undefined; }
    const observer = new IntersectionObserver((entries) => entries.forEach((entry) => {
      if (entry.isIntersecting) { entry.target.classList.add('in'); observer.unobserve(entry.target); }
    }), { threshold: 0.15, rootMargin: '0px 0px -40px 0px' });
    items.forEach((item) => observer.observe(item));
    return () => observer.disconnect();
  }, [rootRef]);
}

/** Pointer spotlight and scroll progress, as CSS variables on the page root. */
function usePageMotion(rootRef) {
  useEffect(() => {
    const root = rootRef.current;
    if (!root || reducedMotion()) return undefined;
    let frame = 0;
    let pointer = null;
    const paint = () => {
      frame = 0;
      const max = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
      root.style.setProperty('--lp-progress', String(Math.min(1, window.scrollY / max)));
      root.style.setProperty('--lp-scroll', String(window.scrollY));
      if (pointer) {
        const box = root.getBoundingClientRect();
        root.style.setProperty('--lp-mx', `${pointer.x - box.left}px`);
        root.style.setProperty('--lp-my', `${pointer.y - box.top}px`);
      }
    };
    const queue = () => { if (!frame) frame = requestAnimationFrame(paint); };
    const move = (event) => { pointer = { x: event.clientX, y: event.clientY }; queue(); };
    window.addEventListener('scroll', queue, { passive: true });
    if (finePointer()) window.addEventListener('pointermove', move, { passive: true });
    paint();
    return () => { window.removeEventListener('scroll', queue); window.removeEventListener('pointermove', move); cancelAnimationFrame(frame); };
  }, [rootRef]);
}

/** Drifting, linked dots behind the hero. Pauses off screen and in a hidden tab. */
function Particles() {
  const canvasRef = useRef(null);
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || reducedMotion()) return undefined;
    const context = canvas.getContext('2d');
    if (!context) return undefined;
    let width = 0;
    let height = 0;
    let points = [];
    let frame = 0;
    let visible = true;
    let pointer = null;
    const resize = () => {
      const ratio = Math.min(2, window.devicePixelRatio || 1);
      width = canvas.clientWidth;
      height = canvas.clientHeight;
      canvas.width = width * ratio;
      canvas.height = height * ratio;
      context.setTransform(ratio, 0, 0, ratio, 0, 0);
      const count = Math.max(18, Math.min(70, Math.round((width * height) / 16000)));
      points = Array.from({ length: count }, () => ({
        x: Math.random() * width, y: Math.random() * height,
        vx: (Math.random() - 0.5) * 0.35, vy: (Math.random() - 0.5) * 0.35, r: 1 + Math.random() * 1.8
      }));
    };
    const draw = () => {
      frame = requestAnimationFrame(draw);
      if (!visible || document.hidden) return;
      context.clearRect(0, 0, width, height);
      for (const p of points) {
        p.x += p.vx; p.y += p.vy;
        if (p.x < -10) p.x = width + 10; if (p.x > width + 10) p.x = -10;
        if (p.y < -10) p.y = height + 10; if (p.y > height + 10) p.y = -10;
        if (pointer) {
          const dx = pointer.x - p.x, dy = pointer.y - p.y, d = Math.hypot(dx, dy);
          if (d < 140 && d > 0) { p.x -= (dx / d) * 0.6; p.y -= (dy / d) * 0.6; }
        }
      }
      for (let i = 0; i < points.length; i++) {
        for (let j = i + 1; j < points.length; j++) {
          const a = points[i], b = points[j], d = Math.hypot(a.x - b.x, a.y - b.y);
          if (d < 120) {
            context.strokeStyle = `rgba(89,100,232,${0.16 * (1 - d / 120)})`;
            context.lineWidth = 1;
            context.beginPath(); context.moveTo(a.x, a.y); context.lineTo(b.x, b.y); context.stroke();
          }
        }
      }
      for (const p of points) {
        context.fillStyle = 'rgba(89,100,232,0.45)';
        context.beginPath(); context.arc(p.x, p.y, p.r, 0, Math.PI * 2); context.fill();
      }
    };
    const sizeObserver = new ResizeObserver(resize);
    sizeObserver.observe(canvas);
    const viewObserver = new IntersectionObserver(([entry]) => { visible = entry.isIntersecting; });
    viewObserver.observe(canvas);
    const move = (event) => { const box = canvas.getBoundingClientRect(); pointer = { x: event.clientX - box.left, y: event.clientY - box.top }; };
    const leave = () => { pointer = null; };
    window.addEventListener('pointermove', move, { passive: true });
    document.addEventListener('pointerleave', leave);
    resize();
    draw();
    return () => {
      cancelAnimationFrame(frame); sizeObserver.disconnect(); viewObserver.disconnect();
      window.removeEventListener('pointermove', move); document.removeEventListener('pointerleave', leave);
    };
  }, []);
  return <canvas ref={canvasRef} className="lp-particles" aria-hidden="true" />;
}

/** Follows the pointer a little (buttons). */
function Magnetic({ as: Tag = 'a', className, children, ...props }) {
  const ref = useRef(null);
  const move = (event) => {
    if (reducedMotion() || !finePointer()) return;
    const box = ref.current.getBoundingClientRect();
    const x = event.clientX - box.left - box.width / 2, y = event.clientY - box.top - box.height / 2;
    ref.current.style.transform = `translate(${x * 0.18}px, ${y * 0.3}px)`;
  };
  const leave = () => { if (ref.current) ref.current.style.transform = ''; };
  return <Tag ref={ref} className={className} onPointerMove={move} onPointerLeave={leave} {...props}>{children}</Tag>;
}

const Arrow = () => <svg className="lp-arrow" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M5 12h14M13 6l6 6-6 6" /></svg>;

/* ---------- hero ---------- */

const SALES = [1240, 860, 2350, 415, 1780, 990, 3120, 640];

function HeroWords({ text, start = 0, className }) {
  return text.split(' ').map((word, index) => <span key={index} className={`lp-word${className ? ' ' + className : ''}`} style={{ '--i': start + index }}>{word}{' '}</span>);
}

function RotatingWord() {
  const words = [L('sales'), L('stock'), L('purchases'), L('suppliers'), L('reports')];
  const [index, setIndex] = useState(0);
  useTicker(() => setIndex((i) => (i + 1) % words.length), 2200);
  return <span className="lp-rotator" aria-live="off">
    <span key={index} className="lp-rotator-word">{words[index]}</span>
  </span>;
}

function HeroDashboard() {
  const stageRef = useRef(null);
  const seen = useInView(stageRef, 0.2);
  const revenue = useCountUp(48294, seen, 1800);
  const orders = useCountUp(128, seen, 1800);
  const [extra, setExtra] = useState({ revenue: 0, orders: 0, sale: 0 });
  useTicker(() => setExtra((current) => {
    const sale = (current.sale + 1) % SALES.length;
    return { revenue: current.revenue + SALES[sale], orders: current.orders + 1, sale };
  }), 2800, seen);

  const tilt = (event) => {
    if (reducedMotion() || !finePointer()) return;
    const box = stageRef.current.getBoundingClientRect();
    const x = (event.clientX - box.left) / box.width - 0.5, y = (event.clientY - box.top) / box.height - 0.5;
    stageRef.current.style.setProperty('--rx', `${(-y * 10).toFixed(2)}deg`);
    stageRef.current.style.setProperty('--ry', `${(x * 12).toFixed(2)}deg`);
  };
  const untilt = () => { stageRef.current.style.setProperty('--rx', '0deg'); stageRef.current.style.setProperty('--ry', '0deg'); };

  const top = [[L('Rice 5kg'), 86], [L('Soybean oil 1L'), 64], [L('Sugar 1kg'), 41]];
  return <div ref={stageRef} className={`lp-stage${seen ? ' in' : ''}`} onPointerMove={tilt} onPointerLeave={untilt}>
    <div className="lp-orbit o1"><i /></div>
    <div className="lp-orbit o2"><i /></div>
    <div className="lp-orbit o3"><i /></div>
    <div className="lp-tilt">
      <div className="lp-dash">
        <div className="lp-dash-top">
          <span>{L('Store overview')}</span>
          <span className="lp-live"><i />{L('Live')}</span>
        </div>
        <div className="lp-dash-stats">
          <div><small>{L('Today’s revenue')}</small><strong>{money(revenue + extra.revenue)}</strong><em className="up">▲ 12.4%</em></div>
          <div><small>{L('Orders')}</small><strong>{Math.round(orders + extra.orders)}</strong><em className="up">▲ 8</em></div>
          <div><small>{L('Low stock')}</small><strong className="warn">3</strong><em className="down">{L('reorder')}</em></div>
        </div>
        <div className="lp-dash-chart">
          <svg viewBox="0 0 400 110" preserveAspectRatio="none" aria-hidden="true">
            <defs>
              <linearGradient id="lpArea" x1="0" x2="0" y1="0" y2="1"><stop offset="0" stopColor="#6d73f0" stopOpacity=".28" /><stop offset="1" stopColor="#6d73f0" stopOpacity="0" /></linearGradient>
              <linearGradient id="lpLine" x1="0" x2="1"><stop offset="0" stopColor="#8b5cf6" /><stop offset="1" stopColor="#5964e8" /></linearGradient>
            </defs>
            {[22, 50, 78].map((y) => <line key={y} x1="0" x2="400" y1={y} y2={y} className="lp-grid-line" />)}
            <path className="lp-area" d="M0 92 C40 84,55 94,88 72 S140 80,174 54 S225 68,258 44 S315 50,345 26 S380 32,400 12 V110 H0Z" fill="url(#lpArea)" />
            <path className="lp-line" pathLength="1" d="M0 92 C40 84,55 94,88 72 S140 80,174 54 S225 68,258 44 S315 50,345 26 S380 32,400 12" fill="none" stroke="url(#lpLine)" strokeWidth="3.2" strokeLinecap="round" />
          </svg>
          <span className="lp-chart-dot" />
        </div>
        <div className="lp-dash-bottom">
          <div className="lp-bars" aria-hidden="true">
            {[42, 64, 38, 80, 56, 92, 70].map((h, i) => <span key={i} style={{ '--h': h, '--i': i }} />)}
          </div>
          <ul className="lp-top-items">
            {top.map(([name, pct], i) => <li key={name}><span>{name}</span><b style={{ '--w': pct, '--i': i }} /></li>)}
          </ul>
        </div>
      </div>
      <div className="lp-chip c1"><span className="lp-chip-icon violet">🧾</span><span><b>{L('Purchase invoice saved')}</b><small>PI-0042 · {L('stock updated')}</small></span></div>
      <div className="lp-chip c2"><span className="lp-chip-icon amber">⚠️</span><span><b>{L('Low stock')}</b><small>{L('Rice 5kg')} · 4 {L('left')}</small></span></div>
      <div className="lp-chip c3" key={extra.sale}><span className="lp-chip-icon green">＋</span><span><b>{money(SALES[extra.sale])} {L('sale')}</b><small>#S-{1040 + extra.orders}</small></span></div>
    </div>
  </div>;
}

/* ---------- animated loops ("gifs") ---------- */

function ScanLoop() {
  const [count, setCount] = useState(24);
  useTicker(() => setCount((c) => (c >= 40 ? 24 : c + 1)), 2400);
  return <figure className="lp-loop">
    <div className="lp-loop-art">
      <svg viewBox="0 0 240 160" aria-hidden="true">
        <defs><filter id="lpGlow" x="-50%" y="-200%" width="200%" height="500%"><feGaussianBlur stdDeviation="2.5" /></filter></defs>
        <polygon className="lpA-beam" points="196,14 86,66 154,66" />
        <rect x="70" y="58" width="100" height="78" rx="9" className="lpA-box" />
        <rect x="70" y="58" width="100" height="16" rx="9" className="lpA-lid" />
        <g className="lpA-code">{[0, 6, 10, 18, 22, 28, 34, 38, 46, 52, 56, 62].map((x, i) => <rect key={x} x={86 + x} y="86" width={i % 3 === 0 ? 4 : 2} height="34" />)}</g>
        <g className="lpA-laser"><rect x="80" y="84" width="80" height="3" rx="1.5" filter="url(#lpGlow)" /><rect x="80" y="84.5" width="80" height="2" rx="1" /></g>
        <g className="lpA-gun"><rect x="186" y="6" width="34" height="18" rx="6" /><rect x="204" y="20" width="10" height="24" rx="4" /></g>
        <text x="178" y="62" className="lpA-plus">+1</text>
      </svg>
    </div>
    <figcaption><b>{L('Scan, add, done')}</b><span>{L('Rice 5kg')} · {L('in stock')} <strong key={count} className="lp-tick">{count}</strong></span></figcaption>
  </figure>;
}

function FlowLoop() {
  const [count, setCount] = useState(118);
  useTicker(() => setCount((c) => (c >= 160 ? 118 : c + 1)), 1500);
  return <figure className="lp-loop">
    <div className="lp-loop-art">
      <svg viewBox="0 0 240 160" aria-hidden="true">
        <rect x="186" y="34" width="44" height="96" rx="6" className="lpB-shelf" />
        {[58, 84, 110].map((y) => <rect key={y} x="190" y={y} width="36" height="3" rx="1.5" className="lpB-board" />)}
        {[0, 1, 2, 3, 4, 5].map((i) => <rect key={i} x={194 + (i % 2) * 17} y={40 + Math.floor(i / 2) * 26} width="14" height="16" rx="2" className="lpB-stack" style={{ '--i': i }} />)}
        <rect x="10" y="112" width="176" height="12" rx="6" className="lpB-belt" />
        <line x1="16" x2="180" y1="118" y2="118" className="lpB-belt-dash" />
        {[24, 62, 100, 138, 172].map((x) => <g key={x} className="lpB-roller" style={{ transformOrigin: `${x}px 132px` }}><circle cx={x} cy="132" r="7" /><line x1={x - 5} x2={x + 5} y1="132" y2="132" /><line x1={x} x2={x} y1="127" y2="137" /></g>)}
        {[0, 1, 2, 3].map((i) => <g key={i} className="lpB-box" style={{ '--i': i }}><rect x="0" y="86" width="26" height="24" rx="3" /><path d="M0 95h26M13 86v9" /></g>)}
      </svg>
    </div>
    <figcaption><b>{L('Stock that updates itself')}</b><span>{L('Purchases in, sales out')} · <strong key={count} className="lp-tick">{count}</strong> {L('units')}</span></figcaption>
  </figure>;
}

function ReceiptLoop() {
  return <figure className="lp-loop">
    <div className="lp-loop-art">
      <svg viewBox="0 0 240 160" aria-hidden="true">
        <rect x="60" y="10" width="120" height="30" rx="8" className="lpC-printer" />
        <rect x="74" y="34" width="92" height="6" rx="3" className="lpC-slot" />
        <circle cx="166" cy="22" r="3" className="lpC-led" />
        <g className="lpC-paper">
          <path d="M80 38h80v104l-6.6 6-6.7-6-6.7 6-6.7-6-6.6 6-6.7-6-6.7 6-6.7-6-6.6 6-6.7-6-6.7 6z" className="lpC-sheet" />
          {[54, 66, 78, 90, 102].map((y, i) => <rect key={y} x="90" y={y} width={i === 4 ? 40 : 60 - (i % 2) * 14} height="4" rx="2" className="lpC-row" style={{ '--i': i }} />)}
          <rect x="90" y="116" width="60" height="1.5" className="lpC-rule" />
          <text x="90" y="132" className="lpC-total">৳ 2,450</text>
        </g>
        <g className="lpC-stamp"><rect x="128" y="104" width="46" height="20" rx="4" /><text x="151" y="118">PAID</text></g>
      </svg>
    </div>
    <figcaption><b>{L('Every sale on record')}</b><span>{L('Print, export to Excel or attach the bill')}</span></figcaption>
  </figure>;
}

/* ---------- sections ---------- */

const FEATURES = [
  { key: 'numbers', icon: 'chart', title: 'Know your numbers', text: 'Revenue, orders and top sellers on one live dashboard, so every decision starts from facts.' },
  { key: 'stock', icon: 'box', title: 'Keep stock in view', text: 'Items, categories and units in one place. Every purchase and sale moves the stock for you.' },
  { key: 'purchase', icon: 'doc', title: 'Purchases with proof', text: 'Record supplier invoices and keep the bill attached, safe and private to your store.' },
  { key: 'alerts', icon: 'bell', title: 'Never run out', text: 'Low-stock alerts point you to what needs reordering before customers notice.' },
  { key: 'team', icon: 'shield', title: 'Your team, your rules', text: 'Give each person the access they need with roles and permissions.' },
  { key: 'language', icon: 'globe', title: 'Speaks your language', text: 'Work in English, বাংলা or हिन्दी — switch any time.' }
];

function FeatureIcon({ name }) {
  const common = { viewBox: '0 0 24 24', width: 24, height: 24, fill: 'none', stroke: 'currentColor', strokeWidth: 2, strokeLinecap: 'round', strokeLinejoin: 'round', 'aria-hidden': true };
  switch (name) {
    case 'chart': return <svg {...common} className="fi fi-chart"><path d="M4 20h16" /><rect x="5" y="11" width="3" height="7" rx="1" /><rect x="10.5" y="7" width="3" height="11" rx="1" /><rect x="16" y="4" width="3" height="14" rx="1" /></svg>;
    case 'box': return <svg {...common} className="fi fi-box"><path d="M3 8l9-5 9 5-9 5z" /><path d="M3 8v8l9 5 9-5V8M12 13v8" /></svg>;
    case 'doc': return <svg {...common} className="fi fi-doc"><path d="M6 3h8l4 4v14H6z" /><path d="M14 3v4h4" /><path className="fi-lines" d="M9 12h6M9 16h4" /></svg>;
    case 'bell': return <svg {...common} className="fi fi-bell"><path d="M6 16V11a6 6 0 0112 0v5l2 2H4z" /><path d="M10 21h4" /></svg>;
    case 'shield': return <svg {...common} className="fi fi-shield"><path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z" /><path className="fi-check" d="M8.5 12l2.5 2.5 4.5-5" /></svg>;
    default: return <svg {...common} className="fi fi-globe"><circle cx="12" cy="12" r="9" /><path d="M3 12h18" /><path className="fi-meridian" d="M12 3c3 3.2 3 14.8 0 18M12 3c-3 3.2-3 14.8 0 18" /></svg>;
  }
}

function Features() {
  const spot = (event) => {
    const card = event.target.closest('.lp-feature');
    if (!card) return;
    const box = card.getBoundingClientRect();
    card.style.setProperty('--x', `${event.clientX - box.left}px`);
    card.style.setProperty('--y', `${event.clientY - box.top}px`);
  };
  return <section className="lp-section" id="features">
    <div className="lp-heading" data-reveal>
      <span className="lp-eyebrow">{L('Everything in one place')}</span>
      <h2>{L('Built for the way stores really work')}</h2>
      <p>{L('From the first delivery to the last sale of the day, StockPilot keeps the whole picture in view.')}</p>
    </div>
    <div className="lp-features" onPointerMove={spot}>
      {FEATURES.map((feature, index) => <article key={feature.key} className={`lp-feature f-${feature.key}`} data-reveal style={{ '--d': index }}>
        <span className="lp-feature-icon"><FeatureIcon name={feature.icon} /></span>
        <h3>{L(feature.title)}</h3>
        <p>{L(feature.text)}</p>
      </article>)}
    </div>
  </section>;
}

function Marquee() {
  const stores = [['🛒', 'Grocery'], ['💊', 'Pharmacy'], ['📱', 'Mobile shop'], ['👗', 'Fashion'], ['🔧', 'Hardware'], ['💄', 'Cosmetics'], ['✏️', 'Stationery'], ['🍞', 'Bakery'], ['🏬', 'Super shop'], ['📚', 'Bookshop']];
  const row = stores.map(([icon, name]) => <span key={name} className="lp-pill"><span aria-hidden="true">{icon}</span>{L(name)}</span>);
  return <div className="lp-marquee" aria-label={L('Made for every kind of store')}>
    <div className="lp-marquee-track"><div>{row}</div><div aria-hidden="true">{row}</div></div>
  </div>;
}

function Steps() {
  const steps = [
    ['Create your store', 'Register your workspace in a minute. No installation, it runs in the browser.'],
    ['Add your items', 'Set up categories, units and prices — or import them from Excel.'],
    ['Buy, sell, relax', 'Record purchases and sales. Stock, reports and alerts take care of themselves.']
  ];
  return <section className="lp-section" id="how">
    <div className="lp-heading" data-reveal>
      <span className="lp-eyebrow">{L('How it works')}</span>
      <h2>{L('Up and running in three steps')}</h2>
    </div>
    <ol className="lp-steps" data-reveal>
      <svg className="lp-steps-line" viewBox="0 0 1000 20" preserveAspectRatio="none" aria-hidden="true"><path pathLength="1" d="M60 10 C300 -6,700 26,940 10" /></svg>
      {steps.map(([title, text], index) => <li key={title} style={{ '--d': index }}>
        <span className="lp-step-no">{index + 1}</span>
        <h3>{L(title)}</h3>
        <p>{L(text)}</p>
      </li>)}
    </ol>
  </section>;
}

function Stat({ value, suffix = '', label, run, index }) {
  const shown = useCountUp(value, run, 1400 + index * 200);
  return <div className="lp-stat" style={{ '--d': index }}><strong>{Math.round(shown)}{suffix}</strong><span>{label}</span></div>;
}

function Stats() {
  const ref = useRef(null);
  const seen = useInView(ref, 0.4);
  return <section ref={ref} className={`lp-stats${seen ? ' in' : ''}`}>
    <Stat value={3} label={L('languages built in')} run={seen} index={0} />
    <Stat value={1} label={L('workspace for your whole store')} run={seen} index={1} />
    <Stat value={24} suffix="/7" label={L('access from any browser')} run={seen} index={2} />
    <Stat value={0} label={L('installs or servers to manage')} run={seen} index={3} />
  </section>;
}

export default function LandingPage() {
  const rootRef = useRef(null);
  useReveal(rootRef);
  usePageMotion(rootRef);

  const line1 = L('Make every day a');
  const highlight = L('good business');
  const line2 = L('day.');
  const words1 = line1.split(' ').length;
  const words2 = highlight.split(' ').length;

  return <div ref={rootRef} className="lp">
    <div className="lp-progress" aria-hidden="true" />
    <div className="lp-bg" aria-hidden="true">
      <span className="lp-blob b1" /><span className="lp-blob b2" /><span className="lp-blob b3" />
      <span className="lp-gridbg" />
      <span className="lp-spot" />
    </div>

    <section className="lp-hero">
      <Particles />
      <div className="lp-floaters" aria-hidden="true">
        <span className="lp-floater f1">📦</span><span className="lp-floater f2">🧾</span><span className="lp-floater f3">📈</span><span className="lp-floater f4">🏷️</span>
      </div>
      <div className="lp-hero-copy">
        <span className="lp-badge"><i />{L('A clearer way to run your store')}</span>
        <h1>
          <HeroWords text={line1} />
          <HeroWords text={highlight} start={words1} className="lp-shine" />
          <HeroWords text={line2} start={words1 + words2} />
        </h1>
        <p className="lp-lead">
          {L('StockPilot keeps your')} <RotatingWord /> {L('in one calm, organised workspace — so you spend less time on busywork and more time with customers.')}
        </p>
        <div className="lp-actions">
          <Magnetic className="lp-btn primary" href="#/register"><span>{L('Create your store')}</span><Arrow /></Magnetic>
          <Magnetic className="lp-btn ghost" href="#/login"><span>{L('Sign in')}</span></Magnetic>
        </div>
        <ul className="lp-trust">
          <li>✓ {L('No installation')}</li>
          <li>✓ {L('Works on phone and desktop')}</li>
          <li>✓ {L('English, বাংলা, हिन्दी')}</li>
        </ul>
      </div>
      <HeroDashboard />
      <a className="lp-scroll-cue" href="#features" aria-label={L('Scroll to features')} onClick={(event) => { event.preventDefault(); document.getElementById('features')?.scrollIntoView({ behavior: reducedMotion() ? 'auto' : 'smooth' }); }}><span /></a>
    </section>

    <Marquee />
    <Features />

    <section className="lp-section">
      <div className="lp-heading" data-reveal>
        <span className="lp-eyebrow">{L('See it in motion')}</span>
        <h2>{L('The busywork, handled')}</h2>
      </div>
      <div className="lp-loops">
        <div data-reveal style={{ '--d': 0 }}><ScanLoop /></div>
        <div data-reveal style={{ '--d': 1 }}><FlowLoop /></div>
        <div data-reveal style={{ '--d': 2 }}><ReceiptLoop /></div>
      </div>
    </section>

    <Steps />
    <Stats />

    <section className="lp-cta" data-reveal>
      <div className="lp-cta-inner">
        <span className="lp-sparkles" aria-hidden="true">{Array.from({ length: 14 }, (_, i) => <i key={i} style={{ '--i': i, left: `${5 + i * 6.6}%`, top: `${12 + ((i * 37) % 76)}%` }} />)}</span>
        <h2>{L('Ready for a calmer store?')}</h2>
        <p>{L('Create your workspace today and have your first items in stock in minutes.')}</p>
        <div className="lp-actions center">
          <Magnetic className="lp-btn light" href="#/register"><span>{L('Create your store')}</span><Arrow /></Magnetic>
          <Magnetic className="lp-btn outline" href="#/login"><span>{L('I already have one')}</span></Magnetic>
        </div>
      </div>
    </section>
  </div>;
}
