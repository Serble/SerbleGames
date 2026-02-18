<template>
  <div v-if="loading" class="animate-pulse space-y-8">
    <div class="h-12 w-1/3 bg-serble-border/20 rounded"></div>
    <div class="h-64 w-full bg-serble-border/20 rounded"></div>
  </div>

  <div v-else-if="game" class="space-y-8">
    <div class="flex flex-col md:flex-row md:items-end justify-between gap-6">
      <div class="flex flex-col md:flex-row gap-6 md:items-center">
        <div class="w-32 h-32 md:w-48 md:h-48 rounded-xl overflow-hidden shadow-2xl bg-serble-card border border-serble-border shrink-0">
          <img 
            :src="getGameIconUrl(game.id)" 
            class="w-full h-full object-cover"
            alt="Game Icon"
            :onError="(e) => e.target.src = DEFAULT_ICON_URL"
          />
        </div>
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
      </div>

      <div class="flex flex-col items-center md:items-end gap-4">
        <div class="text-3xl font-bold text-serble-primary">
          {{ game.price === 0 ? 'FREE' : '$' + game.price.toFixed(2) }}
        </div>
        
        <div v-if="isOwned" class="space-y-4 w-full">
          <div class="flex flex-col items-center md:items-end space-y-1">
            <p class="text-green-500 font-medium">In your library</p>
            <div v-if="game.playtime !== undefined" class="flex flex-col items-center md:items-end text-sm text-serble-text-muted">
              <span class="flex items-center"><Clock class="w-4 h-4 mr-1" /> {{ formatPlaytime(game.playtime) }} played</span>
              <span v-if="game.lastPlayed" class="text-xs">Last played {{ new Date(game.lastPlayed).toLocaleDateString() }}</span>
            </div>
          </div>
          <div class="flex flex-wrap gap-2 justify-center md:justify-end">
            <button v-if="game.windowsRelease" @click="download('windows')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> Windows
            </button>
            <button v-if="game.linuxRelease" @click="download('linux')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> Linux
            </button>
            <button v-if="game.macRelease" @click="download('mac')" class="btn btn-outline flex items-center">
              <Download class="w-4 h-4 mr-2" /> macOS
            </button>
            <p v-if="!game.windowsRelease && !game.linuxRelease && !game.macRelease" class="text-serble-text-muted italic">No packages available yet.</p>
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
          <MarkdownContent :content="game.description" />
        </div>

        <!-- Achievements Section -->
        <div v-if="visibleAchievements.length > 0" class="card p-8">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-2xl font-bold flex items-center">
              <Trophy class="w-6 h-6 mr-2 text-yellow-500" /> Achievements
            </h2>
            <span v-if="isOwned" class="text-sm text-serble-text-muted">
              Earned: {{ earnedAchievements.length }} / {{ achievements.length }}
            </span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div 
              v-for="ach in visibleAchievements" 
              :key="ach.id" 
              class="flex flex-col p-4 rounded-lg border transition-colors relative"
              :class="isEarned(ach.id) ? 'bg-serble-primary/5 border-serble-primary/30' : 'bg-serble-dark/30 border-serble-border opacity-60'"
            >
              <div v-if="ach.hidden" class="absolute top-2 right-2" title="Hidden Achievement">
                <EyeOff class="w-3 h-3 text-yellow-500/50" />
              </div>
              <div class="flex items-center space-x-4 mb-2">
                <div class="w-14 h-14 rounded-lg overflow-hidden shrink-0 border border-serble-border bg-serble-card">
                  <img 
                    :src="getAchievementIconUrl(ach.id)" 
                    class="w-full h-full object-cover"
                    :class="{ 'grayscale': !isEarned(ach.id) }"
                    alt="Achievement Icon"
                    :onError="(e) => e.target.src = DEFAULT_ICON_URL"
                  />
                </div>
                <div class="min-w-0 flex-1">
                  <h4 class="font-bold truncate" :class="{ 'text-serble-primary': isEarned(ach.id) }">{{ ach.title }}</h4>
                  <p class="text-xs text-serble-text-muted line-clamp-2">{{ ach.description }}</p>
                </div>
              </div>
              <div class="text-[9px] text-serble-text-muted font-mono break-all">{{ ach.id }}</div>
            </div>
          </div>
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
                <Monitor v-if="game.windowsRelease" class="w-4 h-4" title="Windows" />
                <Terminal v-if="game.linuxRelease" class="w-4 h-4" title="Linux" />
                <Apple v-if="game.macRelease" class="w-4 h-4" title="macOS" />
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
import { ArrowLeft, Download, Monitor, Terminal, Apple, Trophy, EyeOff, Clock } from 'lucide-vue-next';
import client from '../api/client';
import MarkdownContent from '../components/MarkdownContent.vue';
import { getGameIconUrl, getAchievementIconUrl, DEFAULT_ICON_URL } from '../utils/icons.js';

const route = useRoute();
const router = useRouter();
const game = ref(null);
const loading = ref(true);
const ownerName = ref('Unknown');
const isOwned = ref(false);
const purchasing = ref(false);
const achievements = ref([]);
const earnedAchievements = ref([]);

const formatPlaytime = (minutes) => {
  if (minutes < 60) return `${Math.round(minutes)}m`;
  return `${(minutes / 60).toFixed(1)}h`;
};

const visibleAchievements = computed(() => {
  return achievements.value.filter(ach => !ach.hidden || isEarned(ach.id));
});

const isEarned = (achId) => earnedAchievements.value.some(a => a.id === achId);

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
    const res = await client.get(`/game/public/${route.params.id}`);
    game.value = res.data;
    await Promise.all([fetchOwner(), checkOwnership(), fetchAchievements()]);
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
    const ownedGame = res.data.find(o => o.id === game.value.id);
    if (ownedGame) {
      isOwned.value = true;
      game.value.playtime = ownedGame.playtime;
      game.value.lastPlayed = ownedGame.lastPlayed;
      await fetchEarnedAchievements();
    }
  } catch (e) {
    console.error(e);
  }
};

const fetchAchievements = async () => {
  try {
    const res = await client.get(`/game/${route.params.id}/achievements`);
    achievements.value = res.data;
  } catch (e) {
    console.error(e);
  }
};

const fetchEarnedAchievements = async () => {
  try {
    const res = await client.get(`/game/${route.params.id}/achievements/earned`);
    earnedAchievements.value = res.data;
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
