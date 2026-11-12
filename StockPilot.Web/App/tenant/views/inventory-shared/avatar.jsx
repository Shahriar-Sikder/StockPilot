import { useEffect, useState } from 'react';
import { callService } from '../abp.js';

/** The mockup's avc(): a stable colour per name. */
export function avatarColour(text) {
  let hash = 0;
  for (const character of String(text || '')) hash = (hash * 31 + character.charCodeAt(0)) % 360;
  return `hsl(${hash},55%,48%)`;
}

const initialsOf = (text) => String(text || '').split(' ').filter(Boolean).slice(0, 2).map((word) => word[0]).join('').toUpperCase();

// Pictures already fetched this session, by "service:id:pictureId" (a new PictureId after an upload is a new key).
const pictures = new Map();

/**
 * Loads an entity picture through `${service}.GetPicture` ({ contentType, base64Content } or null) once per
 * session and returns a data: URL, or '' when there is none.
 */
export function usePicture(service, id, pictureId) {
  const key = service && id && pictureId ? `${service}:${id}:${pictureId}` : '';
  const [src, setSrc] = useState(() => (key && typeof pictures.get(key) === 'string' ? pictures.get(key) : ''));
  useEffect(() => {
    if (!key) { setSrc(''); return undefined; }
    let live = true;
    if (!pictures.has(key)) {
      pictures.set(key, callService(service, 'GetPicture', { id })
        .then((picture) => (picture && picture.base64Content ? `data:${picture.contentType};base64,${picture.base64Content}` : ''))
        .catch(() => ''));
    }
    Promise.resolve(pictures.get(key)).then((value) => { pictures.set(key, value); if (live) setSrc(value); });
    return () => { live = false; };
  }, [key]);
  return src;
}

/**
 * The mockup's AV(): the picture when there is one, otherwise coloured initials.
 *   <Avatar name="Green Tea" />                                     initials
 *   <Avatar name={row.itemName} picture={{ service: 'ItemMaster', id: row.id, pictureId: row.pictureId }} />
 *   size="lg" for the 52px drawer avatar; inside a table cell the CSS shrinks it to 28px.
 */
export default function Avatar({ name, src, picture, size = '', style, children }) {
  const loaded = usePicture(picture && picture.service, picture && picture.id, picture && picture.pictureId);
  const image = src || loaded;
  const className = `av ${size}`.trim();
  if (image) return <img className={className} src={image} alt="" />;
  return <div className={className} style={{ background: avatarColour(name), ...style }}>{children ?? initialsOf(name)}</div>;
}
