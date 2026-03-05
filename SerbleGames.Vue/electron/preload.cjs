'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  // ── OAuth ─────────────────────────────────────────────────────────────────
  /**
   * Opens oauthUrl in the system browser, starts a local callback server on
   * port 13580, and resolves with the authorization code when it arrives.
   * @param {string} oauthUrl  Fully-constructed OAuth authorization URL
   * @returns {Promise<string>} Authorization code
   */
  waitForOAuthCode: (oauthUrl) => ipcRenderer.invoke('oauth-wait-for-code', oauthUrl),

  /**
   * Pushes the current auth token and API base URL to the main process so the
   * Game Management Server can make authenticated API calls on behalf of games.
   * Call after login, logout, and on startup if a cached token exists.
   * @param {string|null} token
   * @param {string|null} apiBaseUrl
   */
  setAuthContext: (token, apiBaseUrl) =>
    ipcRenderer.send('set-auth-context', { token, apiBaseUrl }),

  // ── Platform ──────────────────────────────────────────────────────────────
  /** 'win32' | 'linux' | 'darwin' */
  platform: process.platform,

  // ── Install management ────────────────────────────────────────────────────
  /**
   * @param {{ gameId: string, gameName: string, downloadUrl: string,
   *           packageId: string, mainBinary: string,
   *           launchArguments: string, iconUrl?: string }} params
   */
  installGame: (params) => ipcRenderer.invoke('install-game', params),

  /** @param {string} gameId */
  uninstallGame: (gameId) => ipcRenderer.invoke('uninstall-game', gameId),

  /** @param {string} gameId */
  launchGame: (gameId) => ipcRenderer.invoke('launch-game', gameId),

  /** @param {string} gameId */
  killGame: (gameId) => ipcRenderer.invoke('kill-game', gameId),

  /** @param {string} gameId @returns {Promise<boolean>} */
  isInstalled: (gameId) => ipcRenderer.invoke('is-installed', gameId),

  /** @param {string} gameId @returns {Promise<boolean>} */
  isRunning: (gameId) => ipcRenderer.invoke('is-running', gameId),

  /** @param {string} gameId @returns {Promise<string|null>} */
  getInstalledVersion: (gameId) => ipcRenderer.invoke('get-installed-version', gameId),

  /** @param {string} gameId @returns {Promise<object|null>} */
  getInstalledGame: (gameId) => ipcRenderer.invoke('get-installed-game', gameId),

  /** @returns {Promise<object[]>} */
  getInstalledGames: () => ipcRenderer.invoke('get-installed-games'),

  // ── Events: main → renderer ───────────────────────────────────────────────

  /**
   * @param {(data: { gameId: string, achievementId: string,
   *                  achievementTitle: string|null, achievementDescription: string|null,
   *                  achievementHidden: boolean }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onAchievementGranted: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('achievement-granted', handler);
    return () => ipcRenderer.removeListener('achievement-granted', handler);
  },

  /**
   * @param {(data: { gameId: string }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onDownloadProgress: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('download-progress', handler);
    return () => ipcRenderer.removeListener('download-progress', handler);
  },

  /**
   * @param {(data: { gameId: string }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onGameStarted: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('game-started', handler);
    return () => ipcRenderer.removeListener('game-started', handler);
  },

  /**
   * @param {(data: { gameId: string, playtimeMinutes: number }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onGameExited: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('game-exited', handler);
    return () => ipcRenderer.removeListener('game-exited', handler);
  },

  /**
   * @param {(data: { gameId: string }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onInstallComplete: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('install-complete', handler);
    return () => ipcRenderer.removeListener('install-complete', handler);
  },

  /**
   * @param {(data: { gameId: string, error: string }) => void} callback
   * @returns {() => void} unsubscribe
   */
  onInstallError: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('install-error', handler);
    return () => ipcRenderer.removeListener('install-error', handler);
  },
});
