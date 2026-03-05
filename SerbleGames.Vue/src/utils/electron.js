/**
 * Returns true when the Vue app is running inside an Electron renderer process.
 */
export function isElectron() {
  return typeof window !== 'undefined' && !!window.electronAPI;
}

/**
 * Returns the current OS platform in the same format used by the backend:
 * 'windows' | 'linux' | 'mac' | null
 */
export function getElectronPlatform() {
  if (!isElectron()) return null;
  const p = window.electronAPI.platform;
  if (p === 'win32') return 'windows';
  if (p === 'linux') return 'linux';
  if (p === 'darwin') return 'mac';
  return null;
}
