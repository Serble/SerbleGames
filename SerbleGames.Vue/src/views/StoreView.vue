<template>
  <div class="space-y-12">
    <section class="text-center py-12">
      <h1 class="text-5xl font-bold mb-4">Discover your next favorite game.</h1>
      <p class="text-xl text-serble-text-muted mb-8">Browse games uploaded by the community.</p>
      
      <div class="max-w-2xl mx-auto flex gap-2">
        <input 
          v-model="searchQuery" 
          @keyup.enter="search"
          type="text" 
          placeholder="Search for games..." 
          class="input text-lg py-3"
        >
        <button @click="search" class="btn btn-primary px-8">Search</button>
      </div>
    </section>

    <section>
      <div class="flex items-center justify-between mb-8">
        <h2 class="text-2xl font-semibold">Featured Games</h2>
        <div class="flex items-center space-x-4">
          <button 
            @click="prevPage" 
            :disabled="offset === 0"
            class="btn btn-outline p-2"
          >
            <ChevronLeft class="w-5 h-5" />
          </button>
          <span class="text-serble-text-muted">Page {{ currentPage }}</span>
          <button 
            @click="nextPage" 
            :disabled="games.length < limit"
            class="btn btn-outline p-2"
          >
            <ChevronRight class="w-5 h-5" />
          </button>
        </div>
      </div>

      <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        <div v-for="i in 8" :key="i" class="card h-64 animate-pulse bg-serble-border/20"></div>
      </div>
      
      <div v-else-if="games.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        <div 
          v-for="game in games" 
          :key="game.id" 
          class="card hover:border-serble-primary transition-all cursor-pointer group flex flex-col"
          @click="router.push(`/game/${game.id}`)"
        >
          <div class="aspect-video w-full overflow-hidden bg-serble-border/10">
            <img 
              :src="`http://localhost:5240/game/${game.id}/icon`" 
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              alt="Game Icon"
              @error="(e) => e.target.src = '/serble_logo.png'"
            />
          </div>
          <div class="p-6 flex-grow">
            <h3 class="text-xl font-bold mb-2 group-hover:text-serble-primary transition-colors">{{ game.name }}</h3>
            <p class="text-serble-text-muted text-sm line-clamp-3 mb-4">{{ game.description }}</p>
          </div>
          <div class="px-6 py-4 bg-serble-border/10 border-t border-serble-border flex items-center justify-between">
            <span class="text-lg font-bold text-serble-primary">
              {{ game.price === 0 ? 'FREE' : '$' + game.price.toFixed(2) }}
            </span>
            <button class="text-sm font-medium hover:underline flex items-center">
              View Details <ChevronRight class="w-4 h-4 ml-1" />
            </button>
          </div>
        </div>
      </div>

      <div v-else class="text-center py-12 card">
        <p class="text-serble-text-muted text-lg">No games found.</p>
        <button @click="reset" class="btn btn-link text-serble-primary mt-2">Clear search</button>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { ChevronLeft, ChevronRight, Search } from 'lucide-vue-next';
import client from '../api/client';

const router = useRouter();
const games = ref([]);
const loading = ref(true);
const searchQuery = ref('');
const offset = ref(0);
const limit = ref(12);

const currentPage = computed(() => Math.floor(offset.value / limit.value) + 1);

const fetchGames = async () => {
  loading.value = true;
  try {
    let url = `/game/public?offset=${offset.value}&limit=${limit.value}`;
    if (searchQuery.value) {
      url = `/game/search?query=${encodeURIComponent(searchQuery.value)}&offset=${offset.value}&limit=${limit.value}`;
    }
    const response = await client.get(url);
    games.value = response.data;
  } catch (e) {
    console.error('Failed to fetch games', e);
  } finally {
    loading.value = false;
  }
};

const search = () => {
  offset.value = 0;
  fetchGames();
};

const reset = () => {
  searchQuery.value = '';
  offset.value = 0;
  fetchGames();
};

const nextPage = () => {
  if (games.value.length === limit.value) {
    offset.value += limit.value;
    fetchGames();
    window.scrollTo(0, 0);
  }
};

const prevPage = () => {
  if (offset.value >= limit.value) {
    offset.value -= limit.value;
    fetchGames();
    window.scrollTo(0, 0);
  }
};

onMounted(fetchGames);
</script>
