<template>
  <div class="min-h-screen flex flex-col">
    <header class="bg-serble-card border-b border-serble-border sticky top-0 z-50">
      <div class="container mx-auto px-4 py-4 flex items-center justify-between">
        <router-link to="/" class="text-2xl font-bold flex items-center space-x-2">
          <img src="/serble_logo.png" alt="Serble Logo" class="w-8 h-8" />
          <span>Games</span>
        </router-link>

        <nav class="hidden md:flex items-center space-x-6">
          <router-link to="/" class="hover:text-serble-primary transition-colors">Store</router-link>
          <router-link v-if="isAuthenticated" to="/library" class="hover:text-serble-primary transition-colors">Library</router-link>
          <router-link v-if="isAuthenticated" to="/creator" class="hover:text-serble-primary transition-colors">Developer Portal</router-link>
        </nav>

        <div class="flex items-center space-x-4">
          <template v-if="isAuthenticated">
            <router-link to="/account" class="flex items-center space-x-2 hover:text-serble-primary">
              <User class="w-5 h-5" />
              <span class="font-medium hidden sm:inline">{{ username }}</span>
            </router-link>
            <button @click="logout" class="btn btn-outline text-sm">Logout</button>
          </template>
          <template v-else>
            <button @click="login" class="btn btn-primary">Login with Serble</button>
          </template>
        </div>
      </div>
    </header>

    <main class="flex-grow container mx-auto px-4 py-8">
      <router-view />
    </main>

    <footer class="bg-serble-card border-t border-serble-border py-8 mt-12">
      <div class="container mx-auto px-4 text-center text-serble-text-muted">
        <p>&copy; 2026 Serble. All rights reserved.</p>
      </div>
    </footer>
  </div>

  <!-- Achievement toast overlay – rendered outside the layout via Teleport -->
  <AchievementToast />
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { User } from 'lucide-vue-next';
import client from './api/client';
import { API_BASE } from './api/client.js';
import { isElectron } from './utils/electron.js';
import { lastGrantedAchievement } from './utils/achievementEvents.js';
import { useAchievementToasts } from './composables/useAchievementToasts.js';
import AchievementToast from './components/AchievementToast.vue';

const { addToast } = useAchievementToasts();

const router = useRouter();
const username = ref('');
const token = ref(localStorage.getItem('backend_token'));
const isAuthenticated = computed(() => !!token.value);

const CLIENT_ID = import.meta.env.VITE_SERBLE_OAUTH_CLIENT_ID;
const OAUTH_URL = import.meta.env.VITE_SERBLE_OAUTH_URL;
const ELECTRON_CALLBACK_PORT = 13580;

const login = async () => {
  if (isElectron()) {
    // ── Electron: open the real browser, receive code via local server ──────
    const state = Math.random().toString(36).substring(7);
    const redirectUri = `http://localhost:${ELECTRON_CALLBACK_PORT}/callback`;
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: CLIENT_ID,
      redirect_uri: redirectUri,
      scope: 'user_info',
      state,
    });
    const oauthUrl = `${OAUTH_URL}?${params.toString()}`;

    try {
      const code = await window.electronAPI.waitForOAuthCode(oauthUrl);
      const response = await client.post('/auth', { code });
      const newToken = response.data.accessToken;
      localStorage.setItem('backend_token', newToken);
      token.value = newToken;
      window.electronAPI.setAuthContext(newToken, API_BASE);
      await fetchUser();
    } catch (e) {
      console.error('Electron OAuth failed:', e);
      alert('Login failed. Please try again.');
    }
    return;
  }

  // ── Web: redirect the page (existing behaviour) ──────────────────────────
  const state = Math.random().toString(36).substring(7);
  const redirectUri = window.location.origin + '/';
  const params = new URLSearchParams({
    response_type: 'token',
    client_id: CLIENT_ID,
    redirect_uri: redirectUri,
    scope: 'user_info',
    state,
  });
  window.location.href = `${OAUTH_URL}?${params.toString()}`;
};

const logout = () => {
  localStorage.removeItem('backend_token');
  token.value = null;
  username.value = '';
  if (isElectron()) window.electronAPI.setAuthContext(null, null);
  router.push({ name: 'store' });
};

const fetchUser = async () => {
  if (!isAuthenticated.value) return;
  try {
    const response = await client.get('/account');
    username.value = response.data.username;
    // Push the confirmed-valid token to the main process so the GMS can use it.
    if (isElectron()) window.electronAPI.setAuthContext(token.value, API_BASE);
  } catch (e) {
    if (e.response?.status === 401) {
      logout();
    }
  }
};

// ─── Achievement notification listener (Electron only) ───────────────────────
let removeAchievementListener = null;

onUnmounted(() => {
  if (removeAchievementListener) removeAchievementListener();
});

onMounted(async () => {
  // Set up IPC listener for achievement grants so we can toast + refresh the UI.
  if (isElectron()) {
    removeAchievementListener = window.electronAPI.onAchievementGranted((data) => {
      // 1. The overlay toast window handles the visual notification in Electron.
      //    The in-app toast is only used on web.

      // 2. Signal any relevant view (e.g. GameDetailView) to refresh its earned list
      lastGrantedAchievement.gameId        = data.gameId;
      lastGrantedAchievement.achievementId = data.achievementId;
      lastGrantedAchievement.seq++;
    });
  }

  if (!isElectron()) {
    // ── Web: handle OAuth redirect callback ────────────────────────────────
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    if (code) {
      window.history.replaceState({}, document.title, '/');
      try {
        const response = await client.post('/auth', { code });
        const newToken = response.data.accessToken;
        localStorage.setItem('backend_token', newToken);
        token.value = newToken;
        await fetchUser();
      } catch (e) {
        console.error('Auth failed', e);
      }
      return;
    }
  }

  await fetchUser();
});
</script>
