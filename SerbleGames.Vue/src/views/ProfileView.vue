<template>
  <div class="space-y-8">
    <div v-if="loadingUser" class="animate-pulse flex items-center space-x-4">
      <div class="w-16 h-16 bg-serble-border/20 rounded-full"></div>
      <div class="h-8 w-48 bg-serble-border/20 rounded"></div>
    </div>
    <div v-else-if="user" class="flex items-center space-x-4">
      <div class="w-16 h-16 bg-serble-primary rounded-full flex items-center justify-center text-3xl font-bold">
        {{ user.username[0].toUpperCase() }}
      </div>
      <div>
        <h1 class="text-3xl font-bold">{{ user.username }}</h1>
        <p class="text-serble-text-muted">Public Profile</p>
      </div>
    </div>

    <section>
      <h2 class="text-2xl font-semibold mb-6">Games by {{ user?.username || 'this user' }}</h2>
      
      <div v-if="loadingGames" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        <div v-for="i in 4" :key="i" class="card h-64 animate-pulse bg-serble-border/20"></div>
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
              :src="getGameIconUrl(game.id)" 
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              alt="Game Icon"
              :onError="(e) => e.target.src = DEFAULT_ICON_URL"
            />
          </div>
          <div class="p-6 flex-grow">
            <h3 class="text-xl font-bold mb-2 group-hover:text-serble-primary transition-colors">{{ game.name }}</h3>
            <p class="text-serble-text-muted text-sm line-clamp-3 mb-4">{{ markdownToPlainText(game.description) }}</p>
          </div>
          <div class="px-6 py-4 bg-serble-border/10 border-t border-serble-border flex items-center justify-between">
            <span class="text-lg font-bold text-serble-primary">
              {{ game.price === 0 ? 'FREE' : '$' + game.price.toFixed(2) }}
            </span>
          </div>
        </div>
      </div>

      <div v-else class="text-center py-24 card">
        <p class="text-serble-text-muted text-lg">No public games found for this user.</p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import client from '../api/client';
import { markdownToPlainText } from '../utils/markdown.js';
import { getGameIconUrl, DEFAULT_ICON_URL } from '../utils/icons.js';

const route = useRoute();
const router = useRouter();
const user = ref(null);
const games = ref([]);
const loadingUser = ref(true);
const loadingGames = ref(true);

const fetchData = async () => {
  const userId = route.params.id;
  loadingUser.value = true;
  loadingGames.value = true;
  
  try {
    const [userRes, gamesRes] = await Promise.all([
      client.get(`/account/${userId}`),
      client.get(`/game/user/${userId}`)
    ]);
    user.value = userRes.data;
    games.value = gamesRes.data;
  } catch (e) {
    console.error(e);
  } finally {
    loadingUser.value = false;
    loadingGames.value = false;
  }
};

onMounted(fetchData);
</script>
