import { reactive } from 'vue';
import client from '../api/client.js';
import { isElectron, getElectronPlatform } from '../utils/electron.js';

// Module-level shared state
// Shared between all composable instances so every view stays in sync.

/**
 * @type {Record<string, { installed: boolean, running: boolean, installing: boolean, version: string|null }>}
 */
const gameStates = reactive({});

/**
 * @type {Record<string, number>}  0–1 progress, absent when idle
 */
const downloadProgress = reactive({});

let listenersInitialized = false;

function ensureState(gameId) {
  if (!gameStates[gameId]) {
    gameStates[gameId] = {
      installed: false,
      running: false,
      installing: false,
      version: null,
    };
  }
  return gameStates[gameId];
}

function initGlobalListeners() {
  if (listenersInitialized || !isElectron()) return;
  listenersInitialized = true;

  const api = window.electronAPI;

  api.onDownloadProgress(({ gameId, progress }) => {
    downloadProgress[gameId] = progress;
    ensureState(gameId); // ensure state object exists
  });

  api.onInstallComplete(({ gameId }) => {
    const s = ensureState(gameId);
    s.installing = false;
    s.installed = true;
    delete downloadProgress[gameId];
  });

  api.onInstallError(({ gameId, error }) => {
    const s = ensureState(gameId);
    s.installing = false;
    delete downloadProgress[gameId];
    console.error(`[Electron] Install error for ${gameId}:`, error);
  });

  api.onGameStarted(({ gameId }) => {
    ensureState(gameId).running = true;
  });

  api.onGameExited(async ({ gameId, playtimeMinutes }) => {
    ensureState(gameId).running = false;
    if (playtimeMinutes > 0) {
      try {
        await client.post(`/game/${gameId}/playtime`, { minutes: playtimeMinutes });
      } catch (e) {
        console.error('[Electron] Failed to record playtime:', e);
      }
    }
  });
}

// Composable

export function useElectronInstaller() {
  const electronAvailable = isElectron();
  const platform = getElectronPlatform();

  if (electronAvailable) {
    initGlobalListeners();
  }

  // State helpers

  /** Returns the reactive state object for a game, creating it if absent. */
  const getState = (gameId) => ensureState(gameId);

  /** Returns current download progress (0–1) for a game, or null if idle. */
  const getProgress = (gameId) => downloadProgress[gameId] ?? null;

  // Per-game refresh

  const refreshGame = async (gameId) => {
    if (!electronAvailable) return;
    const [installed, running, version] = await Promise.all([
      window.electronAPI.isInstalled(gameId),
      window.electronAPI.isRunning(gameId),
      window.electronAPI.getInstalledVersion(gameId),
    ]);
    const s = ensureState(gameId);
    s.installed = installed;
    s.running = running;
    s.version = version;
  };

  const refreshAll = async (gameIds) => {
    await Promise.all(gameIds.map((id) => refreshGame(id)));
  };

  // Capability checks

  /**
   * Returns true if the current platform has a release for this game.
   * @param {object} game - game object from the API
   */
  const canInstall = (game) => {
    if (!platform) return false;
    return !!game[`${platform}Release`];
  };

  /**
   * Returns true if a newer version of the game is available on the server.
   * @param {object} game
   */
  const isUpdateAvailable = (game) => {
    const s = getState(game.id);
    if (!s.installed || !s.version) return false;
    const latest = game[`${platform}Release`];
    if (!latest) return false;
    return s.version !== latest;
  };

  // Actions

  const install = async (game) => {
    if (!electronAvailable || !platform) return;

    const releaseId = game[`${platform}Release`];
    if (!releaseId) {
      alert(`No ${platform} release is available for "${game.name}".`);
      return;
    }

    const s = ensureState(game.id);
    if (s.installed) {
      alert(`"${game.name}" is already installed.`);
      return;
    }

    s.installing = true;
    downloadProgress[game.id] = 0;

    try {
      const [pkgRes, urlRes] = await Promise.all([
        client.get(`/game/${game.id}/package/${releaseId}`),
        client.get(`/game/${game.id}/download/${platform}`),
      ]);

      const { id: packageId, mainBinary, launchArguments } = pkgRes.data;
      const downloadUrl = urlRes.data;
      // Icon endpoint is AllowAnonymous – pass the URL; main process downloads it
      const iconUrl = `${client.defaults.baseURL}/game/${game.id}/icon`;

      await window.electronAPI.installGame({
        gameId: game.id,
        gameName: game.name,
        downloadUrl,
        packageId,
        mainBinary,
        launchArguments: launchArguments || '',
        iconUrl,
      });
      // install-complete event updates the state
    } catch (e) {
      s.installing = false;
      delete downloadProgress[game.id];
      alert(`Failed to install "${game.name}": ${e.message}`);
    }
  };

  const uninstall = async (gameId, gameName = 'this game') => {
    if (!electronAvailable) return;
    if (!window.confirm(`Uninstall ${gameName}? All local game files will be deleted.`)) return;
    try {
      await window.electronAPI.uninstallGame(gameId);
      const s = ensureState(gameId);
      s.installed = false;
      s.version = null;
    } catch (e) {
      alert(`Failed to uninstall: ${e.message}`);
    }
  };

  const launch = async (gameId) => {
    if (!electronAvailable) return;
    try {
      await window.electronAPI.launchGame(gameId);
    } catch (e) {
      alert(`Failed to launch: ${e.message}`);
    }
  };

  const kill = async (gameId) => {
    if (!electronAvailable) return;
    try {
      await window.electronAPI.killGame(gameId);
    } catch (e) {
      alert(`Failed to stop game: ${e.message}`);
    }
  };

  /**
   * Re-installs the game (uninstalls without confirmation prompt, then installs).
   * @param {object} game
   */
  const update = async (game) => {
    if (!electronAvailable) return;
    try {
      // Uninstall silently (skip confirm)
      await window.electronAPI.uninstallGame(game.id);
      const s = ensureState(game.id);
      s.installed = false;
      s.version = null;
    } catch (e) {
      alert(`Failed to prepare update: ${e.message}`);
      return;
    }
    await install(game);
  };

  return {
    /** Whether we are running inside Electron */
    isElectron: electronAvailable,
    /** 'windows' | 'linux' | 'mac' | null */
    platform,
    getState,
    getProgress,
    canInstall,
    isUpdateAvailable,
    install,
    uninstall,
    launch,
    kill,
    update,
    refreshGame,
    refreshAll,
  };
}
