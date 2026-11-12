// Display formatting, as in the mockup (numbers always en-US style, dates yyyy-mm-dd).

/** The mockup's n(v, d): thousands separators, at most d decimals (default 2). */
export function n(value, decimals = 2) {
  const number = Number(value);
  if (value === null || value === undefined || value === '' || Number.isNaN(number)) return '';
  return number.toLocaleString('en-US', { maximumFractionDigits: decimals });
}

/** Server dates ("2026-11-07T00:00:00") as yyyy-mm-dd. */
export function date(value) {
  if (!value) return '';
  return String(value).slice(0, 10);
}

/** yyyy-mm-dd for today + offset days (the mockup's day()). */
export function day(offset = 0) {
  const d = new Date();
  d.setDate(d.getDate() + offset);
  const pad = (x) => String(x).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/** The mockup's ago(): Today / Yesterday / n days ago (returns [key, count] for localization). */
export function agoParts(value) {
  const days = Math.round((new Date(day(0)) - new Date(date(value))) / 864e5);
  if (days <= 0) return ['Today'];
  if (days === 1) return ['Yesterday'];
  return ['{0} days ago', days];
}

/** Bytes as the mockup's fsz(): "1.2 MB" / "340 KB". */
export function fileSize(bytes) {
  return bytes > 1e6 ? `${n(bytes / 1e6, 1)} MB` : `${n(bytes / 1e3, 0)} KB`;
}
