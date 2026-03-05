<!-- Callback page for the web oauth flow -->

<template>
  <div class="page">
    <div class="card">
      <div v-if="status === 'loading'" class="loadingContainer">
        <div class="callback-spinner">
          ⚡
        </div>
        <h2 class="title">Logging you in...</h2>
        <p class="subtitle">Completing authentication</p>
      </div>

      <div v-else-if="status === 'success'" class="loadingContainer">
        <div :style="{ fontSize: '3rem' }">✓</div>
        <h2 class="title">Welcome back!</h2>
        <p class="subtitle">Redirecting you to the app...</p>
      </div>
  
      <div v-else class="statusError">
        <div class="errorTitle">⚠️ Failed to login</div>
        <div class="errorMessage">We were not able to authenticate you.</div>
        <div class="buttonGroup">
          <button class="btn btn-primary" @click=home>Home</button>
          <button class="btn btn-outline" @click=retry>Retry</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1.5rem;
  padding: 2rem;
}
.card {
  background: #1a1d2e;
  border: 1px solid #2d3148;
  border-radius: 12px;
  padding: 3rem 2.5rem;
  max-width: 480px;
  text-align: center;
  box-shadow: 0 8px 32px rgba(0,0,0,0.4);
}
.loadingContainer {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: '1.5rem';
}
.spinner {
  font-size: 3rem;
  display: inline-block;
}
.title {
  margin: 0;
  font-size: 1.5rem;
  fontWeight: 700;
  color: #f1f5f9;
}
.subtitle { 
  margin: '0.5rem 0 0';
  fontSize: '0.95rem';
  color: '#94a3b8';
  fontWeight: 400;
}
.statusGood {
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1.5rem;
  background: #14532d;
  color: #86efac;
  font-size: 0.9rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.statusError {
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1.5rem;
  //background: #7f1d1d;
  //color: #fca5a5;
  font-size: 0.9rem;
  font-weight: 500,
}
.errorTitle {
  font-size: 1.1rem;
  font-weight: 600;
  color: #fca5a5;
  margin-bottom: 0.5rem;
}
.errorMessage {
  font-size: 0.85rem;
  color: #fecaca;
  margin: 0.5rem 0;
  line-height: 1.5;
}
.buttonGroup {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  margin-top: 1.5rem;
  flex-wrap: wrap;
}
</style>

<script setup>
import { ref, onMounted } from 'vue';
import { isElectron } from '../utils/electron.js';
import { useRouter } from 'vue-router';
import client from '../api/client';
import { useAuth } from '../composables/useAuth.js';

const router = useRouter();
const { username, token, isAuthenticated, fetchUser } = useAuth();

const status = ref('loading');  // 'loading', 'success', 'error'

function home() {
  router.push('/');
}

function retry() {
  window.location.reload();
}

onMounted(async () => {
  if (isElectron()) {
    // achievement get: how did we get here?
    router.push('/');
    return;
  }

  // Check if this is a redirect back from the OAuth flow with a code in the URL
  const params = new URLSearchParams(window.location.search);
  const code = params.get('code');
  if (code) {
    try {
      const response = await client.post('/auth', { code });
      const newToken = response.data.accessToken;
      localStorage.setItem('backend_token', newToken);
      token.value = newToken;
      await fetchUser();
      status.value = "success";
      
      router.push('/');
    } catch (e) {
      console.error('Auth failed', e);
      status.value = 'error';
    }
    return;
  }
  
  status.value = 'error';
});
</script>
