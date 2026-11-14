import { useState } from 'react';

/** The mockup's avc(): a stable colour per name. */
export function avatarColour(text) {
  let hash = 0;
  for (const character of String(text || '')) hash = (hash * 31 + character.charCodeAt(0)) % 360;
  return `hsl(${hash},55%,48%)`;
}

const initialsOf = (text) => String(text || '').split(' ').filter(Boolean).slice(0, 2).map((word) => word[0]).join('').toUpperCase();

/**
 * An item, category or sub-category picture loaded straight from its URL (PictureUrl, e.g. /Pictures/t3/items/….png).
 * onError reports a picture that does not load (deleted file, no access), so the caller can show its fallback.
 */
export function usePictureFailed(src) {
  const [failedSrc, setFailedSrc] = useState(null);
  return [!!src && src === failedSrc, () => setFailedSrc(src)];
}

/**
 * The mockup's AV(): the picture when there is one, otherwise coloured initials.
 *   <Avatar name="Green Tea" />                                  initials
 *   <Avatar name={row.itemName} src={row.pictureUrl} />          the picture, or initials if it is empty or fails to load
 *   size="lg" for the 52px drawer avatar; inside a table cell the CSS shrinks it to 28px.
 */
export default function Avatar({ name, src, size = '', style, children }) {
  const [failed, onError] = usePictureFailed(src);
  const className = `av ${size}`.trim();
  if (src && !failed) return <img className={className} src={src} alt="" onError={onError} />;
  return <div className={className} style={{ background: avatarColour(name), ...style }}>{children ?? initialsOf(name)}</div>;
}
