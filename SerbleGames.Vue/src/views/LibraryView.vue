<template>
  <div class="space-y-8">
    <h1 class="text-3xl font-bold">My Library</h1>

    <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <div v-for="i in 4" :key="i" class="card h-48 animate-pulse bg-serble-border/20"></div>
    </div>

    <div v-else-if="games.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <div 
        v-for="game in games" 
        :key="game.id" 
        class="card group hover:border-serble-primary transition-all flex flex-col overflow-hidden"
      >
        <div class="aspect-video w-full overflow-hidden bg-serble-border/10">
          <img 
            :src="getGameIconUrl(game.id)" 
            class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            alt="Game Icon"
            :onError="(e) => e.target.src = DEFAULT_ICON_URL"
          />
        </div>
        <div class="p-6 flex-grow">
          <h3 class="text-xl font-bold mb-2 group-hover:text-serble-primary transition-colors">{{ game.name }}</h3>
          <p class="text-serble-text-muted text-sm line-clamp-2">{{ markdownToPlainText(game.description) }}</p>
          <div v-if="game.playtime !== undefined" class="mt-4 flex items-center justify-between text-xs text-serble-text-muted">
            <span class="flex items-center"><Clock class="w-3 h-3 mr-1" /> {{ formatPlaytime(game.playtime) }}</span>
            <span v-if="game.lastPlayed" title="Last Played">Last: {{ new Date(game.lastPlayed).toLocaleDateString() }}</span>
          </div>
        </div>
        <div class="p-4 bg-serble-border/10 border-t border-serble-border flex flex-col gap-2">
          <button @click="router.push(`/game/${game.id}`)" class="btn btn-outline text-sm w-full">View Details</button>
          <div class="flex gap-2">
            <button 
              v-if="game.windowsRelease" 
              @click="download(game.id, 'windows')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for Windows"
            >
              <Download class="w-3 h-3 mr-1" /> Win
            </button>
            <button 
              v-if="game.linuxRelease" 
              @click="download(game.id, 'linux')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for Linux"
            >
              <Download class="w-3 h-3 mr-1" /> Lin
            </button>
            <button 
              v-if="game.macRelease" 
              @click="download(game.id, 'mac')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for macOS"
            >
              <Download class="w-3 h-3 mr-1" /> Mac
            </button>
          </div>
          <p v-if="!game.windowsRelease && !game.linuxRelease && !game.macRelease" class="text-[10px] text-center text-serble-text-muted italic">No packages available</p>
        </div>
      </div>
    </div>

    <div v-else class="text-center py-24 card">
      <div class="mb-4 flex justify-center text-serble-text-muted">
        <Library class="w-16 h-16" />
      </div>
      <h2 class="text-2xl font-bold mb-2">Your library is empty</h2>
      <p class="text-serble-text-muted mb-6">Discover amazing games in the store.</p>
      <router-link to="/" class="btn btn-primary">Go to Store</router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Download, Library, Clock } from 'lucide-vue-next';
import client from '../api/client';
import { markdownToPlainText } from '../utils/markdown.js';
import { getGameIconUrl, DEFAULT_ICON_URL } from '../utils/icons.js';

const router = useRouter();
const games = ref([]);
const loading = ref(true);

const formatPlaytime = (minutes) => {
  if (minutes < 60) return `${Math.round(minutes)}m`;
  return `${(minutes / 60).toFixed(1)}h`;
};

const fetchLibrary = async () => {
  loading.value = true;
  try {
    const res = await client.get('/game/owned');
    games.value = res.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const download = async (gameId, platform) => {
  try {
    const res = await client.get(`/game/${gameId}/download/${platform}`);
    window.location.href = res.data;
  } catch (e) {
    alert('Failed to get download link');
  }
};

onMounted(fetchLibrary);
</script>
