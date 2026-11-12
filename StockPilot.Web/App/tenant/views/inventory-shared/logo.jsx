// The StockPilot mark: a stock box with an arrow taking off from it. Drawn on a 24×24 grid in currentColor, so it sits
// in the accent tile (.lg) of the sidebar and sign-in cards. The browser-tab icon (dashboard/public/favicon.svg) is
// the same drawing on a fixed blue tile; the static templates (login, host-login, public layout) inline LOGO_SVG.
export const LOGO_PATHS = '<path d="M3 10.5l6-3 6 3-6 3z"/><path d="M3 10.5v7l6 3 6-3v-7M9 13.5v7"/><path d="M13 9l7.5-5.5M15.5 3.5h5v5"/>';

export const LOGO_SVG = (size = 20) => `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${LOGO_PATHS}</svg>`;

/** The mark in its accent tile: <Logo /> (32px tile, 20px drawing). */
export default function Logo({ size = 20 }) {
  return <div className="lg" dangerouslySetInnerHTML={{ __html: LOGO_SVG(size) }} />;
}
