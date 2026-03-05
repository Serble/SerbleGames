'use strict';

const { app, BrowserWindow, ipcMain, shell, screen, Tray, Menu, nativeImage } = require('electron');
const path = require('path');
const fs = require('fs');
const https = require('https');
const http = require('http');
const { spawn } = require('child_process');
const os = require('os');
const AdmZip = require('adm-zip');

const {
    startGameManagementServer,
    setAuthContext: setGmsAuthContext,
    setMainWindow: setGmsMainWindow,
    setToastCallback,
} = require('./gameManagementServer.cjs');

// ─── Auth context IPC (forwarded to Game Management Server) ──────────────────
// The renderer calls this after every login, logout, and on startup.
ipcMain.on('set-auth-context', (_event, { token, apiBaseUrl }) => {
    setGmsAuthContext(token, apiBaseUrl);
});

// ─── State ───────────────────────────────────────────────────────────────────

let mainWindow = null;
let tray = null;
let isQuitting = false;
let installData = {}; // gameId -> { installed, version, name, path, exe, args, shortcut }
const runningProcesses = {}; // gameId -> { process, startTime }

function getDataFilePath() {
  return path.join(app.getPath('userData'), 'install_data.json');
}

function getInstallBasePath() {
  return path.join(app.getPath('userData'), 'installed_games');
}

function loadData() {
  try {
    const fp = getDataFilePath();
    if (fs.existsSync(fp)) {
      installData = JSON.parse(fs.readFileSync(fp, 'utf8'));
    }
  } catch (e) {
    console.error('Failed to load install data:', e);
    installData = {};
  }
}

function saveData() {
  try {
    fs.writeFileSync(getDataFilePath(), JSON.stringify(installData, null, 2), 'utf8');
  } catch (e) {
    console.error('Failed to save install data:', e);
  }
}

function sendToRenderer(channel, data) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, data);
  }
}

// ─── Achievement overlay toast window ────────────────────────────────────────

const TOAST_WIN_W = 360;
const TOAST_WIN_H = 480;
let toastWindow = null;

function createToastWindow() {
  const { workArea } = screen.getPrimaryDisplay();

  toastWindow = new BrowserWindow({
    width:  TOAST_WIN_W,
    height: TOAST_WIN_H,
    // Anchor to the bottom-right of the usable display area (above the taskbar)
    x: workArea.x + workArea.width  - TOAST_WIN_W - 16,
    y: workArea.y + workArea.height - TOAST_WIN_H - 16,
    transparent:  true,
    frame:        false,
    alwaysOnTop:  true,
    skipTaskbar:  true,
    focusable:    false,   // never steal focus from the running game
    hasShadow:    false,
    resizable:    false,
    movable:      false,
    webPreferences: {
      preload:          path.join(__dirname, 'toastPreload.cjs'),
      contextIsolation: true,
      nodeIntegration:  false,
    },
    show: false,
  });

  // Highest always-on-top level – appears over fullscreen apps / games
  toastWindow.setAlwaysOnTop(true, 'screen-saver');
  // Entire window is click-through so the player can still interact with the game
  toastWindow.setIgnoreMouseEvents(true);

  toastWindow.loadFile(path.join(__dirname, 'toast.html'));

  toastWindow.on('closed', () => { toastWindow = null; });
}

// Show / hide driven by the toast renderer (it knows when the queue is empty)
ipcMain.on('toast-show', () => {
  if (toastWindow && !toastWindow.isDestroyed()) toastWindow.showInactive();
});
ipcMain.on('toast-hide', () => {
  if (toastWindow && !toastWindow.isDestroyed()) toastWindow.hide();
});

// Register the GMS callback – forwards achievement data to the overlay window
setToastCallback((data) => {
  if (!toastWindow || toastWindow.isDestroyed()) return;
  // If the window is still loading, wait for it then send
  if (toastWindow.webContents.isLoading()) {
    toastWindow.webContents.once('did-finish-load', () => {
      toastWindow.webContents.send('add-toast', data);
    });
  } else {
    toastWindow.webContents.send('add-toast', data);
  }
});

// ─── Download helper ─────────────────────────────────────────────────────────

function downloadFile(gameId, url, destPath, silent = false, redirectCount = 0) {
  return new Promise((resolve, reject) => {
    if (redirectCount > 10) {
      reject(new Error('Too many redirects'));
      return;
    }

    let parsedUrl;
    try {
      parsedUrl = new URL(url);
    } catch (e) {
      reject(new Error(`Invalid URL: ${url}`));
      return;
    }

    const proto = parsedUrl.protocol === 'https:' ? https : http;

    proto.get(url, (response) => {
      // Follow redirects
      if (
        response.statusCode >= 300 &&
        response.statusCode < 400 &&
        response.headers.location
      ) {
        response.resume();
        resolve(downloadFile(gameId, response.headers.location, destPath, silent, redirectCount + 1));
        return;
      }

      if (response.statusCode !== 200) {
        response.resume();
        reject(new Error(`HTTP ${response.statusCode}: ${response.statusMessage}`));
        return;
      }

      const dir = path.dirname(destPath);
      fs.mkdirSync(dir, { recursive: true });

      const totalBytes = parseInt(response.headers['content-length'] || '0', 10);
      let downloadedBytes = 0;

      const file = fs.createWriteStream(destPath);

      response.on('data', (chunk) => {
        downloadedBytes += chunk.length;
        if (!silent && totalBytes > 0) {
          sendToRenderer('download-progress', {
            gameId,
            progress: downloadedBytes / totalBytes,
          });
        }
      });

      response.pipe(file);

      file.on('finish', () => file.close(() => resolve(destPath)));

      file.on('error', (err) => {
        try { fs.unlinkSync(destPath); } catch (_) {}
        reject(err);
      });

      response.on('error', (err) => {
        file.close();
        try { fs.unlinkSync(destPath); } catch (_) {}
        reject(err);
      });
    }).on('error', reject);
  });
}

// ─── Zip extraction ──────────────────────────────────────────────────────────

function tryExtractZip(zipPath, destDir) {
  try {
    process.noAsar = true;
    const zip = new AdmZip(zipPath);
    zip.extractAllTo(destDir, true /* overwrite */);
    fs.unlinkSync(zipPath);
    return true;
  } catch (e) {
    console.warn('ZIP extraction failed (treating as raw binary):', e.message);
    return false;
  }
}

// ─── Shortcut helpers ────────────────────────────────────────────────────────

function createShortcut(name, execPath, iconPath) {
  const safeName = name.replace(/[<>:"/\\|?*]/g, '').trim();

  if (os.platform() === 'linux') {
    try {
      const localApps = path.join(os.homedir(), '.local', 'share', 'applications');
      fs.mkdirSync(localApps, { recursive: true });
      const desktopPath = path.join(localApps, `${safeName.toLowerCase().replace(/\s+/g, '-')}.desktop`);
      const content = [
        '[Desktop Entry]',
        'Type=Application',
        `Name=${name}`,
        `Exec="${execPath}"`,
        `Icon=${iconPath || ''}`,
        'Terminal=false',
        'Categories=Game;',
        '',
      ].join('\n');
      fs.writeFileSync(desktopPath, content);
      try {
        const { execSync } = require('child_process');
        execSync(`chmod +x "${desktopPath}"`, { timeout: 5000 });
      } catch (_) {}
      return desktopPath;
    } catch (e) {
      console.error('Failed to create Linux shortcut:', e);
      return null;
    }
  }

  if (os.platform() === 'win32') {
    try {
      const appdata = process.env.APPDATA || '';
      const startMenu = path.join(appdata, 'Microsoft', 'Windows', 'Start Menu', 'Programs');
      const shortcutPath = path.join(startMenu, `${safeName}.lnk`);
      const iconLine = iconPath
        ? `$s.IconLocation = "${iconPath.replace(/\\/g, '\\\\')}"; `
        : '';
      const script =
        `$w = New-Object -ComObject WScript.Shell; ` +
        `$s = $w.CreateShortcut("${shortcutPath.replace(/\\/g, '\\\\')}"); ` +
        `$s.TargetPath = "${execPath.replace(/\\/g, '\\\\')}"; ` +
        iconLine +
        `$s.Save()`;
      const { execSync } = require('child_process');
      execSync(`powershell -NoProfile -NonInteractive -Command "${script}"`, {
        windowsHide: true,
        timeout: 15000,
      });
      return shortcutPath;
    } catch (e) {
      console.error('Failed to create Windows shortcut:', e);
      return null;
    }
  }

  // macOS – no-op for now
  return null;
}

// ─── IPC handlers ────────────────────────────────────────────────────────────

// install-game
ipcMain.handle('install-game', async (_event, params) => {
  const { gameId, gameName, downloadUrl, packageId, mainBinary, launchArguments, iconUrl } = params;

  if (installData[gameId]?.installed) {
    throw new Error('Game is already installed');
  }

  const installDir = path.join(getInstallBasePath(), gameId);
  const compressedPath = path.join(installDir, 'compressed');

  fs.mkdirSync(installDir, { recursive: true });

  try {
    // 1. Download game archive
    await downloadFile(gameId, downloadUrl, compressedPath);

    // 2. Extract (assume zip; fall back to raw binary)
    const extracted = tryExtractZip(compressedPath, installDir);
    if (!extracted) {
      // Not a zip – rename to the declared binary name so launch still works
      const destBin = path.join(installDir, mainBinary);
      fs.mkdirSync(path.dirname(destBin), { recursive: true });
      fs.renameSync(compressedPath, destBin);
    }

    // 3. Download icon (best-effort, AllowAnonymous endpoint)
    let iconPath = null;
    if (iconUrl) {
      const iconDest = path.join(installDir, 'icon.png');
      try {
        await downloadFile(gameId, iconUrl, iconDest, true /* silent */);
        iconPath = iconDest;
      } catch (_) {
        // Not critical
      }
    }

    // 4. Create desktop shortcut
    const exePath = path.join(installDir, mainBinary);
    const shortcutPath = createShortcut(gameName, exePath, iconPath);

    // 5. Persist metadata
    installData[gameId] = {
      installed: true,
      version: packageId,
      name: gameName,
      path: installDir,
      exe: mainBinary,
      args: launchArguments || '',
      shortcut: shortcutPath || '',
    };
    saveData();

    sendToRenderer('install-complete', { gameId });
    return { success: true };
  } catch (e) {
    // Clean up on failure
    try { fs.rmSync(installDir, { recursive: true, force: true }); } catch (_) {}
    sendToRenderer('install-error', { gameId, error: e.message });
    throw e;
  }
});

// uninstall-game
ipcMain.handle('uninstall-game', async (_event, gameId) => {
  const info = installData[gameId];
  if (!info?.installed) throw new Error('Game is not installed');
  if (runningProcesses[gameId]) throw new Error('Game is currently running');

  // Remove shortcut
  if (info.shortcut && fs.existsSync(info.shortcut)) {
    try { fs.unlinkSync(info.shortcut); } catch (e) { console.error(e); }
  }

  // Remove game files
  if (info.path && fs.existsSync(info.path)) {
    fs.rmSync(info.path, { recursive: true, force: true });
  }

  delete installData[gameId];
  saveData();
  return { success: true };
});

// launch-game
ipcMain.handle('launch-game', async (_event, gameId) => {
  const info = installData[gameId];
  if (!info?.installed) throw new Error('Game is not installed');
  if (runningProcesses[gameId]) throw new Error('Game is already running');

  const exePath = path.join(info.path, info.exe);
  if (!fs.existsSync(exePath)) {
    throw new Error(`Executable not found: ${exePath}`);
  }

  // Ensure executable bit on Unix
  if (os.platform() !== 'win32') {
    try { fs.chmodSync(exePath, 0o755); } catch (_) {}
  }

  const args = info.args ? info.args.split(/\s+/).filter(Boolean) : [];
  const proc = spawn(exePath, args, {
    cwd: info.path,
    detached: false,
    stdio: 'ignore',
  });

  const startTime = Date.now();
  runningProcesses[gameId] = { process: proc, startTime };
  sendToRenderer('game-started', { gameId });

  proc.on('exit', () => {
    const playtimeMinutes = Math.floor((Date.now() - startTime) / 60000);
    delete runningProcesses[gameId];
    sendToRenderer('game-exited', { gameId, playtimeMinutes });
  });

  return { success: true };
});

// kill-game
ipcMain.handle('kill-game', async (_event, gameId) => {
  const entry = runningProcesses[gameId];
  if (!entry) throw new Error('Game is not running');
  entry.process.kill();
  return { success: true };
});

// is-installed
ipcMain.handle('is-installed', (_event, gameId) => {
  return !!(installData[gameId]?.installed);
});

// is-running
ipcMain.handle('is-running', (_event, gameId) => {
  return !!runningProcesses[gameId];
});

// get-installed-version
ipcMain.handle('get-installed-version', (_event, gameId) => {
  return installData[gameId]?.version ?? null;
});

// get-installed-game
ipcMain.handle('get-installed-game', (_event, gameId) => {
  return installData[gameId] ?? null;
});

// get-installed-games
ipcMain.handle('get-installed-games', () => {
  return Object.entries(installData)
    .filter(([, v]) => v.installed)
    .map(([id, v]) => ({ id, ...v }));
});

// ─── OAuth callback server ────────────────────────────────────────────────────

const OAUTH_CALLBACK_PORT = 13580;

/**
 * Opens the OAuth URL in the system browser and waits for the authorization
 * code to be delivered to a temporary local HTTP server on port 13580.
 * Resolves with the code string, or rejects on error / timeout.
 */
ipcMain.handle('oauth-wait-for-code', (_event, oauthUrl) => {
  return new Promise((resolve, reject) => {
    let settled = false;

    const done = (err, code) => {
      if (settled) return;
      settled = true;
      server.close();
      if (err) reject(err);
      else resolve(code);
    };

    const server = http.createServer((req, res) => {
      // Only handle GET /callback
      const parsed = new URL(req.url, `http://127.0.0.1:${OAUTH_CALLBACK_PORT}`);
      if (parsed.pathname !== '/callback') {
        res.writeHead(404);
        res.end();
        return;
      }

      const code = parsed.searchParams.get('code');

      const html = code
        ? `<!DOCTYPE html><html><body style="font-family:sans-serif;text-align:center;padding:60px;background:#1a1a2e;color:#e0e0e0">
            <h2 style="color:#7c5cbf">&#10003; Login successful!</h2>
            <p>You can close this tab and return to Serble Games.</p>
            <script>window.close();<\/script>
           </body></html>`
        : `<!DOCTYPE html><html><body style="font-family:sans-serif;text-align:center;padding:60px;background:#1a1a2e;color:#e0e0e0">
            <h2 style="color:#e05c5c">&#10007; Login failed</h2>
            <p>No authorization code was received. Please try again.</p>
           </body></html>`;

      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
      res.end(html);

      if (code) {
        done(null, code);
      } else {
        done(new Error('OAuth callback did not include an authorization code'));
      }
    });

    server.on('error', (err) => {
      done(new Error(`OAuth callback server error: ${err.message}`));
    });

    // 5-minute timeout
    const timeout = setTimeout(() => {
      done(new Error('OAuth login timed out after 5 minutes'));
    }, 5 * 60 * 1000);

    server.on('close', () => clearTimeout(timeout));

    server.listen(OAUTH_CALLBACK_PORT, '127.0.0.1', () => {
      // Server is ready – open the browser
      shell.openExternal(oauthUrl).catch((err) => {
        done(new Error(`Could not open browser: ${err.message}`));
      });
    });
  });
});

// ─── System tray ─────────────────────────────────────────────────────────────

function createTray() {
  const iconPath = app.isPackaged
    ? path.join(__dirname, '../dist/serble_logo.png')
    : path.join(__dirname, '../public/serble_logo.png');

  let icon;
  try {
    icon = nativeImage.createFromPath(iconPath).resize({ width: 16, height: 16 });
  } catch (_) {
    icon = nativeImage.createEmpty();
  }

  tray = new Tray(icon);
  tray.setToolTip('Serble Games');

  const buildMenu = () => Menu.buildFromTemplate([
    {
      label: 'Open Serble Games',
      click: () => {
        if (mainWindow) {
          mainWindow.show();
          mainWindow.focus();
        } else {
          createWindow();
        }
      },
    },
    { type: 'separator' },
    {
      label: 'Quit',
      click: () => {
        isQuitting = true;
        app.quit();
      },
    },
  ]);

  tray.setContextMenu(buildMenu());

  // Left-click on tray icon shows/focuses the window
  tray.on('click', () => {
    if (mainWindow) {
      if (mainWindow.isVisible()) {
        mainWindow.focus();
      } else {
        mainWindow.show();
        mainWindow.focus();
      }
    } else {
      createWindow();
    }
  });
}

// ─── Window creation ─────────────────────────────────────────────────────────

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    title: 'Serble Games',
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
    },
  });

  if (!app.isPackaged) {
    // Dev: load from Vite dev server
    mainWindow.loadURL('http://localhost:3000');
    mainWindow.webContents.openDevTools();
  } else {
    // Production: load built files
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'));
  }

  // Give the GMS a reference so it can push events to the renderer.
  setGmsMainWindow(mainWindow);

  // Intercept close: hide to tray instead of quitting, unless we're actually quitting.
  mainWindow.on('close', (event) => {
    if (!isQuitting) {
      event.preventDefault();
      mainWindow.hide();
    }
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  loadData();
  startGameManagementServer();
  createToastWindow();
  createTray();
  createWindow();

  app.on('activate', () => {
    // On macOS, re-show window when dock icon is clicked and no windows are open
    if (mainWindow) {
      mainWindow.show();
    } else {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  // Do NOT quit — keep the GMS and install services running in the tray.
  // The user must select "Quit" from the tray icon to fully exit.
});
