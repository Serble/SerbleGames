'use strict';

/**
 * Game Management Server
 *
 * TCP server on 127.0.0.1:46599 that games connect to via the Serble Games SDK.
 * Implements complete protocol parity with the Godot launcher's GameManagementServer.cs.
 *
 * Wire format (all integers big-endian):
 *   [4 bytes: body_length]  [body_length bytes: type(4) + data(body_length-4)]
 *
 * Packet types:
 *   0  HandshakePacket          client → server  data: string(gameId)
 *   1  HandshakeResponsePacket  server → client  data: bool(isLoggedIn) + prefixedOptional(username)
 *   2  AckPacket                server → client  data: (empty)
 *   3  GrantAchievementPacket   client → server  data: string(achievementId)
 */

const net = require('net');
const https = require('https');
const http = require('http');

// ─── Auth context (set by renderer via IPC after login / logout) ──────────────

let _authToken = null;
let _apiBaseUrl = null;

/**
 * Called from main.cjs via ipcMain to update the stored credentials
 * whenever the renderer logs in, logs out, or confirms a cached token.
 *
 * @param {string|null} token
 * @param {string|null} apiBaseUrl
 */
function setAuthContext(token, apiBaseUrl) {
    _authToken = token || null;
    _apiBaseUrl = apiBaseUrl || null;
}

// ─── Toast callback (set by main.cjs to push events to the overlay window) ───

let _onToast = null;

/**
 * Register a function that will be called with achievement data whenever a new
 * achievement is granted.  Used by main.cjs to forward the event to the
 * always-on-top toast BrowserWindow.
 * @param {((data: object) => void) | null} fn
 */
function setToastCallback(fn) {
    _onToast = fn || null;
}

// ─── Renderer reference (for pushing events to the UI) ───────────────────────

let _mainWindow = null;

/**
 * Must be called from createWindow() in main.cjs so the GMS can push events
 * to the renderer (e.g. achievement notifications).
 * @param {import('electron').BrowserWindow} win
 */
function setMainWindow(win) {
    _mainWindow = win;
}

function sendToRenderer(channel, data) {
    if (_mainWindow && !_mainWindow.isDestroyed()) {
        _mainWindow.webContents.send(channel, data);
    }
}

// ─── Simple API helper ────────────────────────────────────────────────────────

/**
 * Makes an authenticated JSON request to the backend.
 * @param {'GET'|'POST'|'PATCH'|'DELETE'} method
 * @param {string} path  e.g. '/account'
 * @param {object|null} body
 * @returns {Promise<any>}
 */
function apiRequest(method, path, body = null) {
    return new Promise((resolve, reject) => {
        if (!_authToken || !_apiBaseUrl) {
            reject(new Error('Not authenticated'));
            return;
        }

        let url;
        try {
            url = new URL(path, _apiBaseUrl);
        } catch (_) {
            reject(new Error(`Invalid URL: ${_apiBaseUrl}${path}`));
            return;
        }

        const proto = url.protocol === 'https:' ? https : http;
        const bodyStr = body != null ? JSON.stringify(body) : null;

        const options = {
            hostname: url.hostname,
            port: url.port || (url.protocol === 'https:' ? 443 : 80),
            path: url.pathname + url.search,
            method,
            headers: {
                Authorization: `Bearer ${_authToken}`,
                'Content-Type': 'application/json',
                ...(bodyStr ? { 'Content-Length': Buffer.byteLength(bodyStr) } : {}),
            },
        };

        const req = proto.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    try { resolve(JSON.parse(data)); } catch (_) { resolve(data); }
                } else {
                    reject(new Error(`HTTP ${res.statusCode}: ${data.slice(0, 200)}`));
                }
            });
        });

        req.on('error', reject);
        if (bodyStr) req.write(bodyStr);
        req.end();
    });
}

// ─── Sequential async buffer ──────────────────────────────────────────────────

/**
 * Accumulates raw socket data chunks and lets callers await exact byte counts
 * in sequence, without manual state machines.
 */
class DataBuffer {
    constructor() {
        this._buf = Buffer.alloc(0);
        this._waiters = []; // { count, resolve, reject }
        this._destroyed = false;
        this._destroyErr = null;
    }

    push(chunk) {
        if (this._destroyed) return;
        this._buf = Buffer.concat([this._buf, chunk]);
        this._flush();
    }

    _flush() {
        while (this._waiters.length > 0) {
            const w = this._waiters[0];
            if (this._buf.length >= w.count) {
                this._waiters.shift();
                const slice = this._buf.slice(0, w.count);
                this._buf = this._buf.slice(w.count);
                w.resolve(slice);
            } else {
                break;
            }
        }
    }

    /** Resolves with exactly `count` bytes when they arrive. */
    readBytes(count) {
        if (this._destroyed) {
            return Promise.reject(this._destroyErr || new Error('Connection closed'));
        }
        return new Promise((resolve, reject) => {
            this._waiters.push({ count, resolve, reject });
            this._flush();
        });
    }

    destroy(err) {
        if (this._destroyed) return;
        this._destroyed = true;
        this._destroyErr = err || new Error('Connection closed');
        for (const w of this._waiters) w.reject(this._destroyErr);
        this._waiters = [];
    }
}

// ─── Packet codec ─────────────────────────────────────────────────────────────

const PACKET_HANDSHAKE          = 0;
const PACKET_HANDSHAKE_RESPONSE = 1;
const PACKET_ACK                = 2;
const PACKET_GRANT_ACHIEVEMENT  = 3;

/** Big-endian signed 32-bit integer → 4-byte Buffer */
function encodeInt32(value) {
    const b = Buffer.alloc(4);
    b.writeInt32BE(value, 0);
    return b;
}

/** 4 UTF-8 length prefix bytes + string bytes */
function encodeString(value) {
    const str = Buffer.from(value, 'utf8');
    return Buffer.concat([encodeInt32(str.length), str]);
}

/** 1-byte boolean */
function encodeBool(value) {
    return Buffer.from([value ? 0x01 : 0x00]);
}

/**
 * Builds a complete wire frame for a packet.
 * @param {number} type
 * @param {Buffer} [data]
 */
function buildFrame(type, data = Buffer.alloc(0)) {
    // body = type(4) + data
    const body = Buffer.concat([encodeInt32(type), data]);
    // wire = body_length(4) + body
    return Buffer.concat([encodeInt32(body.length), body]);
}

/**
 * Reads one packet from the DataBuffer.
 * Returns { type: number, data: Buffer }
 */
async function readPacket(buf) {
    const lenBuf   = await buf.readBytes(4);
    const bodyLen  = lenBuf.readInt32BE(0);
    const bodyBuf  = await buf.readBytes(bodyLen);
    const type     = bodyBuf.readInt32BE(0);
    const data     = bodyLen > 4 ? bodyBuf.slice(4) : Buffer.alloc(0);
    return { type, data };
}

/**
 * Decodes a length-prefixed UTF-8 string from a Buffer starting at `offset`.
 * Returns { value: string, end: number }
 */
function decodeString(buf, offset = 0) {
    const len = buf.readInt32BE(offset);
    const value = buf.slice(offset + 4, offset + 4 + len).toString('utf8');
    return { value, end: offset + 4 + len };
}

// ─── Outgoing packet builders ─────────────────────────────────────────────────

/**
 * HandshakeResponsePacket (type 1)
 * data: bool(isLoggedIn) + bool(hasUsername) [+ string(username) if hasUsername]
 */
function buildHandshakeResponse(isLoggedIn, username) {
    const parts = [encodeBool(isLoggedIn)];
    if (username != null) {
        parts.push(encodeBool(true));      // hasUsername prefix
        parts.push(encodeString(username));
    } else {
        parts.push(encodeBool(false));     // hasUsername prefix
    }
    return buildFrame(PACKET_HANDSHAKE_RESPONSE, Buffer.concat(parts));
}

/** AckPacket (type 2) – no data */
function buildAckFrame() {
    return buildFrame(PACKET_ACK);
}

// ─── Achievement logic ────────────────────────────────────────────────────────

/**
 * Mirrors Global.GrantAchievement from the Godot launcher:
 *  1. Skip if already earned.
 *  2. Get the current user's ID.
 *  3. POST grant.
 */
async function grantAchievement(gameId, achievementId) {
    if (!_authToken) {
        console.warn('[GMS] Cannot grant achievement: not authenticated');
        return;
    }

    // 1. Skip if already earned
    try {
        const earned = await apiRequest('GET', `/game/${gameId}/achievements/earned`);
        if (Array.isArray(earned) && earned.some((a) => a.id === achievementId)) {
            console.log(`[GMS] Achievement ${achievementId} already earned – skipping`);
            return;
        }
    } catch (e) {
        console.warn('[GMS] Could not check earned achievements:', e.message);
        // Proceed anyway – worst case is a duplicate grant attempt the backend will reject.
    }

    // 2. Get user ID
    const account = await apiRequest('GET', '/account');
    const userId = account.id;

    // 3. Grant
    await apiRequest('POST', `/game/achievement/${achievementId}/grant/${userId}`);
    console.log(`[GMS] Granted achievement ${achievementId} for game ${gameId} to user ${userId}`);

    // 4. Fetch achievement details for the UI notification (AllowAnonymous endpoint)
    let achievementTitle = null;
    let achievementDescription = null;
    let achievementHidden = false;
    try {
        const allAchievements = await apiRequest('GET', `/game/${gameId}/achievements`);
        const ach = Array.isArray(allAchievements)
            ? allAchievements.find((a) => a.id === achievementId)
            : null;
        if (ach) {
            achievementTitle       = ach.title       || null;
            achievementDescription = ach.description || null;
            achievementHidden      = ach.hidden       || false;
        }
    } catch (e) {
        console.warn('[GMS] Could not fetch achievement details for notification:', e.message);
    }

    // 5. Notify the renderer so it can refresh the earned achievements UI
    //    and fire the toast callback for the overlay window.
    const iconUrl = _apiBaseUrl
        ? `${_apiBaseUrl}/game/achievement/${achievementId}/icon`
        : null;

    const eventData = {
        gameId,
        achievementId,
        achievementTitle,
        achievementDescription,
        achievementHidden,
        iconUrl,
    };

    sendToRenderer('achievement-granted', eventData);
    if (_onToast) _onToast(eventData);
}

// ─── Per-connection handler ───────────────────────────────────────────────────

async function handleClient(socket) {
    const remote = `${socket.remoteAddress}:${socket.remotePort}`;
    const buf = new DataBuffer();

    socket.on('data',  (chunk) => buf.push(chunk));
    socket.on('close', ()      => buf.destroy());
    socket.on('error', (err)   => buf.destroy(err));

    let authenticated = false;
    let handshakeGameId = null;

    try {
        // Process packets until the socket closes.
        while (!buf._destroyed) {
            const { type, data } = await readPacket(buf);
            console.log(`[GMS] ${remote} packet type=${type}`);

            // First packet MUST be a handshake
            if (!authenticated && type !== PACKET_HANDSHAKE) {
                throw new Error(`Expected HandshakePacket (0), got ${type}`);
            }

            switch (type) {
                // ── Handshake ────────────────────────────────────────────────
                case PACKET_HANDSHAKE: {
                    const { value: gameId } = decodeString(data);
                    handshakeGameId = gameId;
                    authenticated = true;

                    const isLoggedIn = !!_authToken;
                    let username = null;

                    if (isLoggedIn) {
                        try {
                            const account = await apiRequest('GET', '/account');
                            username = account.username || null;
                        } catch (e) {
                            console.warn('[GMS] Could not fetch account for handshake:', e.message);
                        }
                    }

                    socket.write(buildHandshakeResponse(isLoggedIn, username));
                    console.log(`[GMS] Handshake OK – game="${gameId}" loggedIn=${isLoggedIn}`);
                    break;
                }

                // ── Grant achievement ─────────────────────────────────────────
                case PACKET_GRANT_ACHIEVEMENT: {
                    const { value: achievementId } = decodeString(data);

                    // Always send Ack regardless of grant outcome (mirrors Godot implementation).
                    try {
                        await grantAchievement(handshakeGameId, achievementId);
                    } catch (e) {
                        console.error(`[GMS] Failed to grant achievement ${achievementId}:`, e.message);
                    }

                    socket.write(buildAckFrame());
                    break;
                }

                default:
                    console.warn(`[GMS] ${remote} unknown packet type ${type} – ignoring`);
                    break;
            }
        }
    } catch (e) {
        const msg = e.message || '';
        // Suppress noisy expected-close errors.
        if (!msg.includes('Connection closed') && !msg.includes('closed')) {
            console.error(`[GMS] ${remote} error:`, msg);
        }
    } finally {
        socket.destroy();
    }

    console.log(`[GMS] ${remote} disconnected`);
}

// ─── Server ───────────────────────────────────────────────────────────────────

const GMS_PORT = 46599;

/**
 * Starts the Game Management Server.
 * Call once from main.cjs after app.whenReady().
 * @returns {net.Server}
 */
function startGameManagementServer() {
    const server = net.createServer((socket) => {
        console.log(`[GMS] Accepted connection from ${socket.remoteAddress}:${socket.remotePort}`);
        handleClient(socket).catch((e) =>
            console.error('[GMS] Unhandled error in client handler:', e)
        );
    });

    server.on('error', (err) => {
        if (err.code === 'EADDRINUSE') {
            console.warn(
                `[GMS] Port ${GMS_PORT} is already in use – ` +
                `another launcher instance may be running. GMS will not start.`
            );
        } else {
            console.error('[GMS] Server error:', err);
        }
    });

    server.listen(GMS_PORT, '127.0.0.1', () => {
        console.log(`[GMS] Listening on 127.0.0.1:${GMS_PORT}`);
    });

    return server;
}

module.exports = { startGameManagementServer, setAuthContext, setMainWindow, setToastCallback };
