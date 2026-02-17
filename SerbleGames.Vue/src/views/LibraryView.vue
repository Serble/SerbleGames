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
        class="card group hover:border-serble-primary transition-all flex flex-col"
      >
        <div class="p-6 flex-grow">
          <h3 class="text-xl font-bold mb-2 group-hover:text-serble-primary transition-colors">{{ game.name }}</h3>
          <p class="text-serble-text-muted text-sm line-clamp-2">{{ game.description }}</p>
        </div>
        <div class="p-4 bg-serble-border/10 border-t border-serble-border flex flex-col gap-2">
          <button @click="router.push(`/game/${game.id}`)" class="btn btn-outline text-sm w-full">View Details</button>
          <div class="flex gap-2">
            <button 
              v-if="game.windowsBuild" 
              @click="download(game.id, 'windows')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for Windows"
            >
              <Download class="w-3 h-3 mr-1" /> Win
            </button>
            <button 
              v-if="game.linuxBuild" 
              @click="download(game.id, 'linux')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for Linux"
            >
              <Download class="w-3 h-3 mr-1" /> Lin
            </button>
            <button 
              v-if="game.macBuild" 
              @click="download(game.id, 'mac')" 
              class="btn btn-primary text-xs flex-1 px-1 flex items-center justify-center"
              title="Download for macOS"
            >
              <Download class="w-3 h-3 mr-1" /> Mac
            </button>
          </div>
          <p v-if="!game.windowsBuild && !game.linuxBuild && !game.macBuild" class="text-[10px] text-center text-serble-text-muted italic">No builds available</p>
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
import { Download, Library } from 'lucide-vue-next';
import client from '../api/client';

const router = useRouter();
const games = ref([]);
const loading = ref(true);

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
