<template>
  <div v-if="loading" class="animate-pulse space-y-8">
    <div class="h-12 w-1/3 bg-serble-border/20 rounded"></div>
    <div class="h-64 w-full bg-serble-border/20 rounded"></div>
  </div>

  <div v-else-if="game" class="space-y-8">
    <div class="flex flex-col md:flex-row md:items-end justify-between gap-6">
      <div class="space-y-2">
        <button @click="router.back()" class="text-serble-primary hover:underline flex items-center mb-4">
          <ArrowLeft class="w-4 h-4 mr-1" /> Back
        </button>
        <h1 class="text-4xl font-bold">{{ game.name }}</h1>
        <p class="text-serble-text-muted">
          Published on {{ new Date(game.publishDate).toLocaleDateString() }} by 
          <router-link :to="`/profile/${game.ownerId}`" class="text-serble-primary hover:underline">
            {{ ownerName }}
          </router-link>
        </p>
      </div>

      <div class="flex flex-col items-center md:items-end gap-4">
        <div class="text-3xl font-bold text-serble-primary">
          {{ game.price === 0 ? 'FREE' : '$' + game.price.toFixed(2) }}
        </div>
        
        <div v-if="isOwned" class="space-y-4 w-full">
          <p class="text-center md:text-right text-green-500 font-medium">In your library</p>
          <div class="flex flex-wrap gap-2 justify-center md:justify-end">
            <button v-if="game.windowsBuild" @click="download('windows')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> Windows
            </button>
            <button v-if="game.linuxBuild" @click="download('linux')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> Linux
            </button>
            <button v-if="game.macBuild" @click="download('mac')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> macOS
            </button>
            <p v-if="!game.windowsBuild && !game.linuxBuild && !game.macBuild" class="text-serble-text-muted italic">No builds available yet.</p>
          </div>
        </div>
        <button v-else @click="purchase" :disabled="purchasing" class="btn btn-primary px-12 py-3 text-lg">
          {{ purchasing ? 'Adding...' : 'Add to Library' }}
        </button>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-12">
      <div class="lg:col-span-2 space-y-8">
        <div class="card p-8">
          <h2 class="text-2xl font-bold mb-4">About this game</h2>
          <p class="whitespace-pre-wrap leading-relaxed">{{ game.description }}</p>
        </div>

        <div v-if="game.trailerVideo" class="card p-8">
          <h2 class="text-2xl font-bold mb-4">Trailer</h2>
          <div class="aspect-video">
            <iframe 
              class="w-full h-full rounded-lg"
              :src="embedUrl" 
              frameborder="0" 
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
              allowfullscreen
            ></iframe>
          </div>
        </div>
      </div>

      <div class="space-y-6">
        <div class="card p-6">
          <h3 class="font-bold mb-4">Game Details</h3>
          <div class="space-y-3 text-sm">
            <div class="flex justify-between">
              <span class="text-serble-text-muted">Platform</span>
              <span class="flex gap-2">
                <Monitor v-if="game.windowsBuild" class="w-4 h-4" title="Windows" />
                <Terminal v-if="game.linuxBuild" class="w-4 h-4" title="Linux" />
                <Apple v-if="game.macBuild" class="w-4 h-4" title="macOS" />
              </span>
            </div>
            <div class="flex justify-between">
              <span class="text-serble-text-muted">Release Date</span>
              <span>{{ new Date(game.publishDate).toLocaleDateString() }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="text-center py-24 card">
    <h2 class="text-2xl font-bold mb-2">Game not found</h2>
    <router-link to="/" class="text-serble-primary hover:underline">Return to Store</router-link>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ArrowLeft, Download, Monitor, Terminal, Apple } from 'lucide-vue-next';
import client from '../api/client';

const route = useRoute();
const router = useRouter();
const game = ref(null);
const loading = ref(true);
const ownerName = ref('Unknown');
const isOwned = ref(false);
const purchasing = ref(false);

const embedUrl = computed(() => {
  if (!game.value?.trailerVideo) return '';
  let videoId = '';
  if (game.value.trailerVideo.includes('v=')) videoId = game.value.trailerVideo.split('v=')[1].split('&')[0];
  else videoId = game.value.trailerVideo.split('/').pop();
  return `https://www.youtube.com/embed/${videoId}`;
});

const fetchGame = async () => {
  loading.value = true;
  try {
    const res = await client.get(`/game/public`);
    const found = res.data.find(g => g.id === route.params.id);
    if (found) {
      game.value = found;
      await Promise.all([fetchOwner(), checkOwnership()]);
    }
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const fetchOwner = async () => {
  try {
    const res = await client.get(`/account/${game.value.ownerId}`);
    ownerName.value = res.data.username;
  } catch (e) {
    console.error(e);
  }
};

const checkOwnership = async () => {
  if (!localStorage.getItem('backend_token')) return;
  try {
    const res = await client.get('/game/owned');
    isOwned.value = res.data.some(o => o.id === game.value.id);
  } catch (e) {
    console.error(e);
  }
};

const purchase = async () => {
  if (!localStorage.getItem('backend_token')) {
    alert('Please login to add games to your library.');
    return;
  }
  purchasing.value = true;
  try {
    await client.post(`/game/${game.value.id}/purchase`);
    isOwned.value = true;
    alert('Game added to your library!');
  } catch (e) {
    alert(e.response?.data || 'Failed to add game to library');
  } finally {
    purchasing.value = false;
  }
};

const download = async (platform) => {
  try {
    const res = await client.get(`/game/${game.value.id}/download/${platform}`);
    window.location.href = res.data;
  } catch (e) {
    alert('Failed to get download link');
  }
};

onMounted(fetchGame);
</script>
