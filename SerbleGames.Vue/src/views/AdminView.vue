<template>
  <div class="max-w-4xl mx-auto space-y-10">
    <div class="flex items-center justify-between">
      <h1 class="text-3xl font-bold">Admin Console</h1>
      <button @click="refreshAll" class="btn btn-outline">Refresh</button>
    </div>

    <div v-if="error" class="card p-6 text-red-400 border border-red-500/40">
      {{ error }}
    </div>

    <div class="card p-6 space-y-4">
      <h2 class="text-xl font-semibold">Server Options</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <label class="flex items-center gap-3">
          <input v-model="optionsForm.requireCreateWhitelist" type="checkbox" class="w-4 h-4 rounded border-serble-border bg-serble-dark text-serble-primary">
          <span class="text-sm">Require whitelist to create games</span>
        </label>
        <label class="flex items-center gap-3">
          <input v-model="optionsForm.requirePaidCreateWhitelist" type="checkbox" class="w-4 h-4 rounded border-serble-border bg-serble-dark text-serble-primary">
          <span class="text-sm">Require whitelist to create paid games</span>
        </label>
        <div class="space-y-2">
          <label class="text-xs text-serble-text-muted">Max games per user (0 = unlimited)</label>
          <input v-model.number="optionsForm.maxGamesPerUser" type="number" min="0" class="input">
        </div>
        <div class="space-y-2">
          <label class="text-xs text-serble-text-muted">Max builds per game (0 = unlimited)</label>
          <input v-model.number="optionsForm.maxBuildsPerGame" type="number" min="0" class="input">
        </div>
      </div>
      <div class="flex justify-end">
        <button @click="saveOptions" :disabled="savingOptions" class="btn btn-primary">
          {{ savingOptions ? 'Saving...' : 'Save Options' }}
        </button>
      </div>
    </div>

    <div class="card p-6 space-y-6">
      <h2 class="text-xl font-semibold">User Management</h2>
      <div class="flex flex-col md:flex-row gap-3">
        <input v-model.trim="lookupUserId" type="text" class="input flex-1" placeholder="User ID">
        <button @click="fetchUser" class="btn btn-primary" :disabled="loadingUser">{{ loadingUser ? 'Loading...' : 'Load User' }}</button>
      </div>

      <div v-if="user" class="space-y-6">
        <div class="flex flex-wrap items-center gap-4">
          <div>
            <div class="text-lg font-bold">{{ user.username }}</div>
            <div class="text-xs text-serble-text-muted">{{ user.id }}</div>
          </div>
          <span v-if="user.isAdmin" class="text-xs rounded border border-serble-primary/60 px-2 py-1 text-serble-primary">Admin</span>
          <span v-if="user.isBanned" class="text-xs rounded border border-red-500/60 px-2 py-1 text-red-400">Banned</span>
        </div>

        <div class="space-y-2">
          <label class="text-xs text-serble-text-muted">Permissions (comma separated)</label>
          <input v-model="permissionsInput" type="text" class="input" placeholder="creator, moderator">
          <div class="flex gap-3">
            <button @click="savePermissions" class="btn btn-outline">Save Permissions</button>
          </div>
        </div>

        <div class="flex flex-wrap gap-3">
          <button @click="toggleAdmin" class="btn btn-outline">
            {{ user.isAdmin ? 'Remove Admin' : 'Make Admin' }}
          </button>
          <button @click="toggleBan" class="btn btn-outline">
            {{ user.isBanned ? 'Unban User' : 'Ban User' }}
          </button>
          <button @click="impersonate" class="btn btn-outline">Login As User</button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="card p-4 bg-serble-dark/40 border border-serble-border space-y-2">
            <div class="font-semibold text-sm">Whitelist: Create Games</div>
            <div class="text-xs text-serble-text-muted">Status: {{ user.whitelistedCreateGames ? 'Whitelisted' : 'Not whitelisted' }}</div>
            <button @click="toggleWhitelist('CreateGames')" class="btn btn-outline btn-sm">
              {{ user.whitelistedCreateGames ? 'Remove' : 'Add' }}
            </button>
          </div>
          <div class="card p-4 bg-serble-dark/40 border border-serble-border space-y-2">
            <div class="font-semibold text-sm">Whitelist: Create Paid Games</div>
            <div class="text-xs text-serble-text-muted">Status: {{ user.whitelistedCreatePaidGames ? 'Whitelisted' : 'Not whitelisted' }}</div>
            <button @click="toggleWhitelist('CreatePaidGames')" class="btn btn-outline btn-sm">
              {{ user.whitelistedCreatePaidGames ? 'Remove' : 'Add' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="card p-6 space-y-4">
      <h2 class="text-xl font-semibold">Whitelists</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div class="space-y-2">
          <div class="text-sm font-semibold">Create Games</div>
          <div class="text-xs text-serble-text-muted" v-if="whitelistCreateGames.length === 0">No users in whitelist.</div>
          <ul v-else class="text-xs text-serble-text-muted space-y-1">
            <li v-for="id in whitelistCreateGames" :key="id">{{ id }}</li>
          </ul>
        </div>
        <div class="space-y-2">
          <div class="text-sm font-semibold">Create Paid Games</div>
          <div class="text-xs text-serble-text-muted" v-if="whitelistCreatePaidGames.length === 0">No users in whitelist.</div>
          <ul v-else class="text-xs text-serble-text-muted space-y-1">
            <li v-for="id in whitelistCreatePaidGames" :key="id">{{ id }}</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue';
import client from '../api/client';

const error = ref('');
const lookupUserId = ref('');
const loadingUser = ref(false);
const user = ref(null);
const permissionsInput = ref('');
const savingOptions = ref(false);
const whitelistCreateGames = ref([]);
const whitelistCreatePaidGames = ref([]);

const optionsForm = reactive({
  requireCreateWhitelist: false,
  requirePaidCreateWhitelist: false,
  maxGamesPerUser: 0,
  maxBuildsPerGame: 0
});

const loadOptions = async () => {
  const res = await client.get('/admin/options');
  Object.assign(optionsForm, res.data);
};

const saveOptions = async () => {
  savingOptions.value = true;
  try {
    await client.put('/admin/options', optionsForm);
  } catch (e) {
    error.value = e.response?.data || 'Failed to save server options.';
  } finally {
    savingOptions.value = false;
  }
};

const fetchUser = async () => {
  if (!lookupUserId.value) return;
  loadingUser.value = true;
  error.value = '';
  try {
    const res = await client.get(`/admin/users/${lookupUserId.value}`);
    user.value = res.data;
    permissionsInput.value = (user.value.permissions || []).join(', ');
  } catch (e) {
    error.value = e.response?.data || 'Failed to load user.';
    user.value = null;
  } finally {
    loadingUser.value = false;
  }
};

const savePermissions = async () => {
  if (!user.value) return;
  const permissions = permissionsInput.value
    .split(',')
    .map(p => p.trim())
    .filter(Boolean);

  try {
    await client.put(`/admin/users/${user.value.id}/permissions`, { permissions });
    await fetchUser();
  } catch (e) {
    error.value = e.response?.data || 'Failed to update permissions.';
  }
};

const toggleBan = async () => {
  if (!user.value) return;
  try {
    await client.post(`/admin/users/${user.value.id}/ban`, { isBanned: !user.value.isBanned });
    await fetchUser();
  } catch (e) {
    error.value = e.response?.data || 'Failed to update ban state.';
  }
};

const toggleAdmin = async () => {
  if (!user.value) return;
  try {
    await client.post(`/admin/users/${user.value.id}/admin`, { isAdmin: !user.value.isAdmin });
    await fetchUser();
  } catch (e) {
    error.value = e.response?.data || 'Failed to update admin state.';
  }
};

const impersonate = async () => {
  if (!user.value) return;
  try {
    const res = await client.post(`/admin/users/${user.value.id}/impersonate`);
    localStorage.setItem('backend_token', res.data.token);
    window.location.href = '/';
  } catch (e) {
    error.value = e.response?.data || 'Failed to impersonate user.';
  }
};

const toggleWhitelist = async (type) => {
  if (!user.value) return;
  const key = type === 'CreateGames' ? 'whitelistedCreateGames' : 'whitelistedCreatePaidGames';
  const nextState = !user.value[key];
  try {
    await client.put(`/admin/whitelist/${user.value.id}`, { isWhitelisted: nextState }, { params: { type } });
    await fetchUser();
    await fetchWhitelists();
  } catch (e) {
    error.value = e.response?.data || 'Failed to update whitelist.';
  }
};

const fetchWhitelists = async () => {
  const [createGames, createPaid] = await Promise.all([
    client.get('/admin/whitelist', { params: { type: 'CreateGames' } }),
    client.get('/admin/whitelist', { params: { type: 'CreatePaidGames' } })
  ]);
  whitelistCreateGames.value = createGames.data;
  whitelistCreatePaidGames.value = createPaid.data;
};

const refreshAll = async () => {
  error.value = '';
  try {
    await Promise.all([loadOptions(), fetchWhitelists()]);
  } catch (e) {
    error.value = e.response?.data || 'Failed to refresh admin data.';
  }
};

onMounted(refreshAll);
</script>
