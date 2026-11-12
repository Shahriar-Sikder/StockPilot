// HTTP for endpoints that are NOT ABP app services: sign-in (/api/Account/Authenticate) and the MVC file
// upload / download actions. App services go through callService() in abp.js.
export const TOKEN_KEY = 'stockpilot.accessToken';
export const USER_KEY = 'stockpilot.user';

/** Saves a Blob as a normal browser download. */
export function saveBlob(fileName, blob) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/**
 * Downloads a FileDto returned by an export method (…ToExcel, GetTemplate) from FileController.DownloadTempFile.
 * The action needs the bearer token, which a plain link cannot send, so the file is fetched and then saved.
 */
export async function downloadTempFile(file) {
  const query = new URLSearchParams({ fileType: file.fileType, fileToken: file.fileToken, fileName: file.fileName });
  let token = null;
  try { token = localStorage.getItem(TOKEN_KEY); } catch (_) { /* storage may be blocked */ }
  const response = await fetch(`/File/DownloadTempFile?${query}`, { credentials: 'same-origin', headers: token ? { Authorization: `Bearer ${token}` } : {} });
  if (!response.ok) throw new Error(`The file could not be downloaded (${response.status}).`);
  saveBlob(file.fileName, await response.blob());
}

/** Saves base64 content (attachments, pictures) as a download. */
export function saveBase64(fileName, contentType, base64) {
  const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
  saveBlob(fileName, new Blob([bytes], { type: contentType || 'application/octet-stream' }));
}

/** fetch with the bearer token; returns ABP's `result` and throws Error(message) on failure. */
export async function api(path, options = {}) {
  let token = null;
  try { token = localStorage.getItem(TOKEN_KEY); } catch (_) { /* storage may be blocked */ }
  const isForm = typeof FormData !== 'undefined' && options.body instanceof FormData;
  const headers = { Accept: 'application/json', ...(options.body && !isForm ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers };
  const response = await fetch(path, { ...options, headers, credentials: 'same-origin' });
  let payload = {};
  try { payload = await response.json(); } catch (_) { /* empty or non-JSON response */ }
  if (!response.ok || payload.success === false || payload.error) {
    const error = new Error(payload.error?.message || payload.error?.Message || payload.message || `Request failed (${response.status})`);
    error.details = payload.error?.details;
    error.status = response.status;
    throw error;
  }
  return payload.result ?? payload.Result ?? payload;
}
