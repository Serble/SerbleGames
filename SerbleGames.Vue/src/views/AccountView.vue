<template>
  <div class="max-w-2xl mx-auto space-y-8">
    <h1 class="text-3xl font-bold">Account Settings</h1>

    <div v-if="loading" class="card p-8 animate-pulse space-y-4">
      <div class="h-8 w-48 bg-serble-border/20 rounded"></div>
      <div class="h-4 w-64 bg-serble-border/20 rounded"></div>
    </div>

    <div v-else-if="user" class="card">
      <div class="p-8 border-b border-serble-border flex items-center space-x-6">
        <div class="w-20 h-20 bg-serble-primary rounded-full flex items-center justify-center text-4xl font-bold">
          {{ user.username[0].toUpperCase() }}
        </div>
        <div>
          <h2 class="text-2xl font-bold">{{ user.username }}</h2>
          <p class="text-serble-text-muted">User ID: {{ user.id }}</p>
        </div>
      </div>
      
      <div class="p-8 space-y-6">
        <div class="space-y-2">
          <h3 class="font-semibold">Linked Serble Account</h3>
          <p class="text-serble-text-muted text-sm">Your account is linked with Serble. Your username and profile information are managed there.</p>
        </div>

        <div class="pt-6 border-t border-serble-border">
          <button @click="logout" class="btn btn-danger">Logout from Serble Games</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import client from '../api/client';

const router = useRouter();
const user = ref(null);
const loading = ref(true);

const fetchUser = async () => {
  try {
    const res = await client.get('/account');
    user.value = res.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const logout = () => {
  localStorage.removeItem('backend_token');
  window.location.href = '/';
};

onMounted(fetchUser);
</script>
