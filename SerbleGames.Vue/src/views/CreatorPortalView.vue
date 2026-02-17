<template>
  <div class="space-y-8">
    <div class="flex items-center justify-between">
      <h1 class="text-3xl font-bold">Developer Portal</h1>
      <button @click="openCreateModal" class="btn btn-primary flex items-center">
        <Plus class="w-4 h-4 mr-2" /> Create New Game
      </button>
    </div>

    <div v-if="loading" class="grid grid-cols-1 gap-4">
      <div v-for="i in 3" :key="i" class="card h-32 animate-pulse bg-serble-border/20"></div>
    </div>

    <div v-else-if="games.length > 0" class="space-y-4">
      <div v-for="game in games" :key="game.id" class="card p-6 flex items-center justify-between">
        <div class="space-y-1">
          <div class="flex items-center space-x-2">
            <h3 class="text-xl font-bold">{{ game.name }}</h3>
            <span :class="game.public ? 'bg-green-500/10 text-green-500' : 'bg-yellow-500/10 text-yellow-500'" class="text-[10px] uppercase font-bold px-2 py-0.5 rounded border border-current">
              {{ game.public ? 'Public' : 'Private' }}
            </span>
          </div>
          <p class="text-serble-text-muted text-sm">{{ game.id }}</p>
          <div class="flex items-center space-x-4 text-xs text-serble-text-muted mt-2">
            <span class="flex items-center"><Monitor class="w-3 h-3 mr-1" /> {{ game.windowsBuild ? 'Ready' : 'No build' }}</span>
            <span class="flex items-center"><Terminal class="w-3 h-3 mr-1" /> {{ game.linuxBuild ? 'Ready' : 'No build' }}</span>
            <span class="flex items-center"><Apple class="w-3 h-3 mr-1" /> {{ game.macBuild ? 'Ready' : 'No build' }}</span>
          </div>
        </div>
        <div class="flex items-center space-x-2">
          <button @click="openReleaseManager(game)" class="btn btn-outline text-sm">Manage Releases</button>
          <button @click="openEditModal(game)" class="btn btn-outline p-2"><Edit2 class="w-4 h-4" /></button>
          <button @click="deleteGame(game)" class="btn btn-outline p-2 hover:bg-red-600 hover:border-red-600 group">
            <Trash2 class="w-4 h-4 group-hover:text-white" />
          </button>
        </div>
      </div>
    </div>

    <div v-else class="text-center py-24 card">
      <p class="text-serble-text-muted text-lg">You haven't created any games yet.</p>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/80 backdrop-blur-sm z-[100] flex items-center justify-center p-4">
      <div class="card w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div class="p-6 border-b border-serble-border flex justify-between items-center sticky top-0 bg-serble-card">
          <h2 class="text-2xl font-bold">{{ editingId ? 'Edit Game' : 'Create New Game' }}</h2>
          <button @click="showModal = false" class="text-serble-text-muted hover:text-white"><X class="w-6 h-6" /></button>
        </div>
        <form @submit.prevent="saveGame" class="p-6 space-y-6">
          <div class="space-y-2">
            <label class="text-sm font-medium text-serble-text-muted">Game Icon</label>
            <div class="flex items-center space-x-4">
              <div class="w-20 h-20 rounded-lg overflow-hidden bg-serble-dark border border-serble-border shrink-0">
                <img 
                  v-if="editingId"
                  :src="`http://localhost:5240/game/${editingId}/icon?t=` + iconRefreshTag" 
                  class="w-full h-full object-cover"
                  alt="Current Icon"
                  @error="(e) => e.target.src = '/serble_logo.png'"
                />
                <div v-else class="w-full h-full flex items-center justify-center text-serble-text-muted">
                  <ImageIcon class="w-8 h-8" />
                </div>
              </div>
              <div class="space-y-2">
                <input type="file" id="icon-upload" class="hidden" @change="handleIconChange" accept="image/*">
                <label for="icon-upload" class="btn btn-outline text-sm cursor-pointer flex items-center">
                  <Upload class="w-4 h-4 mr-2" /> {{ editingId ? 'Change Icon' : 'Select Icon' }}
                </label>
                <p v-if="selectedIcon" class="text-xs text-serble-text-muted">{{ selectedIcon.name }}</p>
              </div>
            </div>
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium text-serble-text-muted">Game Name</label>
            <input v-model="form.name" type="text" required class="input" placeholder="My Awesome Game">
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium text-serble-text-muted">Description</label>
            <textarea v-model="form.description" required rows="4" class="input py-2" placeholder="Tell the world about your game..."></textarea>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="space-y-2">
              <label class="text-sm font-medium text-serble-text-muted">Price (USD)</label>
              <input v-model.number="form.price" type="number" step="0.01" required class="input" placeholder="0.00">
            </div>
            <div class="space-y-2">
              <label class="text-sm font-medium text-serble-text-muted">Publish Date (Optional)</label>
              <input v-model="form.publishDate" type="date" class="input">
            </div>
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium text-serble-text-muted">Trailer Video (YouTube URL)</label>
            <input v-model="form.trailerVideo" type="url" class="input" placeholder="https://www.youtube.com/watch?v=...">
          </div>
          <div class="flex items-center space-x-3">
            <input v-model="form.public" type="checkbox" id="is-public" class="w-4 h-4 rounded border-serble-border bg-serble-dark text-serble-primary focus:ring-serble-primary">
            <label for="is-public" class="text-sm font-medium">Publicly listed</label>
          </div>
          <div class="flex justify-end space-x-4 pt-4 border-t border-serble-border">
            <button type="button" @click="showModal = false" class="btn btn-outline">Cancel</button>
            <button type="submit" :disabled="saving" class="btn btn-primary px-8">
              {{ saving ? 'Saving...' : (editingId ? 'Update Game' : 'Create Game') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Release Manager Modal -->
    <div v-if="managingGame" class="fixed inset-0 bg-black/80 backdrop-blur-sm z-[100] flex items-center justify-center p-4">
      <div class="card w-full max-w-3xl overflow-hidden">
        <div class="p-6 border-b border-serble-border flex justify-between items-center bg-serble-card">
          <div>
            <h2 class="text-2xl font-bold">Manage Releases</h2>
            <p class="text-serble-text-muted text-sm">{{ managingGame.name }}</p>
          </div>
          <button @click="managingGame = null" class="text-serble-text-muted hover:text-white"><X class="w-6 h-6" /></button>
        </div>
        <div class="p-6 space-y-8 max-h-[70vh] overflow-y-auto">
          <div class="flex border-b border-serble-border">
            <button @click="releaseTab = 'releases'" :class="releaseTab === 'releases' ? 'border-b-2 border-serble-primary text-serble-primary' : 'text-serble-text-muted'" class="px-4 py-2 font-medium">Builds</button>
            <button @click="releaseTab = 'achievements'" :class="releaseTab === 'achievements' ? 'border-b-2 border-serble-primary text-serble-primary' : 'text-serble-text-muted'" class="px-4 py-2 font-medium">Achievements</button>
          </div>

          <div v-if="releaseTab === 'releases'" class="space-y-8">
            <div v-for="plat in ['windows', 'linux', 'mac']" :key="plat" class="space-y-4">
              <div class="flex items-center justify-between">
                <div class="flex items-center space-x-3">
                  <Monitor v-if="plat === 'windows'" class="w-6 h-6" />
                  <Terminal v-if="plat === 'linux'" class="w-6 h-6" />
                  <Apple v-if="plat === 'mac'" class="w-6 h-6" />
                  <h4 class="font-bold capitalize">{{ plat }} Release</h4>
                </div>
                <span v-if="managingGame[plat + 'Build']" class="text-xs text-green-500 flex items-center">
                  <Check class="w-3 h-3 mr-1" /> Current Build: {{ managingGame[plat + 'Build'].substring(0, 8) }}...
                </span>
                <span v-else class="text-xs text-serble-text-muted italic">No build uploaded</span>
              </div>
              
              <div class="flex items-center space-x-4">
                <input type="file" :id="'file-' + plat" class="hidden" @change="e => handleFileChange(plat, e)">
                <label :for="'file-' + plat" class="btn btn-outline text-sm cursor-pointer flex items-center">
                  <Upload class="w-4 h-4 mr-2" /> Select File
                </label>
                <button 
                  @click="uploadRelease(plat)" 
                  :disabled="uploading === plat || !selectedFiles[plat]" 
                  class="btn btn-primary text-sm"
                >
                  {{ uploading === plat ? 'Uploading...' : 'Upload Build' }}
                </button>
                <span v-if="selectedFiles[plat]" class="text-xs text-serble-text-muted truncate max-w-[200px]">
                  {{ selectedFiles[plat].name }}
                </span>
              </div>
              <div v-if="uploadStatus[plat]" :class="uploadStatus[plat].error ? 'text-red-500' : 'text-green-500'" class="text-xs">
                {{ uploadStatus[plat].msg }}
              </div>
            </div>
          </div>

          <div v-else class="space-y-6">
            <div class="flex justify-between items-center">
              <h4 class="font-bold">Game Achievements</h4>
              <button @click="openAchievementModal()" class="btn btn-primary btn-sm">Add Achievement</button>
            </div>

            <div v-if="achievements.length > 0" class="space-y-4">
              <div v-for="ach in achievements" :key="ach.id" class="card p-4 flex items-center justify-between bg-serble-dark/50">
                <div class="flex items-center space-x-4">
                  <div class="w-12 h-12 rounded bg-serble-card border border-serble-border overflow-hidden shrink-0">
                    <img :src="`http://localhost:5240/game/achievement/${ach.id}/icon`" class="w-full h-full object-cover" @error="(e) => e.target.src = '/serble_logo.png'" />
                  </div>
                  <div>
                    <div class="flex items-center space-x-2">
                      <h5 class="font-bold">{{ ach.title }}</h5>
                      <span v-if="ach.hidden" class="text-[8px] uppercase border border-yellow-500 text-yellow-500 px-1 rounded">Hidden</span>
                    </div>
                    <p class="text-xs text-serble-text-muted line-clamp-1">{{ ach.description }}</p>
                  </div>
                </div>
                <div class="flex items-center space-x-2">
                  <button @click="openAchievementModal(ach)" class="btn btn-outline p-1.5"><Edit2 class="w-3.5 h-3.5" /></button>
                  <button @click="deleteAchievement(ach.id)" class="btn btn-outline p-1.5 hover:bg-red-600 hover:border-red-600 group">
                    <Trash2 class="w-3.5 h-3.5 group-hover:text-white" />
                  </button>
                </div>
              </div>
            </div>
            <p v-else class="text-center py-8 text-serble-text-muted italic">No achievements created for this game.</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Achievement Create/Edit Modal -->
    <div v-if="showAchievementModal" class="fixed inset-0 bg-black/90 backdrop-blur-sm z-[110] flex items-center justify-center p-4">
      <div class="card w-full max-w-lg">
        <div class="p-6 border-b border-serble-border flex justify-between items-center">
          <h3 class="text-xl font-bold">{{ editingAchievementId ? 'Edit Achievement' : 'Add Achievement' }}</h3>
          <button @click="showAchievementModal = false" class="text-serble-text-muted hover:text-white"><X class="w-6 h-6" /></button>
        </div>
        <form @submit.prevent="saveAchievement" class="p-6 space-y-4">
          <div class="flex items-center space-x-4">
            <div class="w-16 h-16 rounded bg-serble-dark border border-serble-border overflow-hidden shrink-0">
              <img v-if="editingAchievementId" :src="`http://localhost:5240/game/achievement/${editingAchievementId}/icon?t=` + achIconRefreshTag" class="w-full h-full object-cover" @error="(e) => e.target.src = '/serble_logo.png'" />
              <div v-else class="w-full h-full flex items-center justify-center text-serble-text-muted"><ImageIcon class="w-6 h-6" /></div>
            </div>
            <div class="space-y-2">
              <input type="file" id="ach-icon-upload" class="hidden" @change="handleAchIconChange" accept="image/*">
              <label for="ach-icon-upload" class="btn btn-outline text-xs cursor-pointer flex items-center">
                <Upload class="w-3 h-3 mr-2" /> Select Icon
              </label>
              <p v-if="selectedAchIcon" class="text-[10px] text-serble-text-muted">{{ selectedAchIcon.name }}</p>
            </div>
          </div>
          <div class="space-y-1">
            <label class="text-xs font-medium text-serble-text-muted">Title</label>
            <input v-model="achForm.title" type="text" required class="input" placeholder="Achievement Title">
          </div>
          <div class="space-y-1">
            <label class="text-xs font-medium text-serble-text-muted">Description</label>
            <textarea v-model="achForm.description" required rows="2" class="input py-2" placeholder="How to earn this..."></textarea>
          </div>
          <div class="flex items-center space-x-3">
            <input v-model="achForm.hidden" type="checkbox" id="ach-hidden" class="w-4 h-4 rounded border-serble-border bg-serble-dark text-serble-primary">
            <label for="ach-hidden" class="text-sm font-medium">Hidden achievement</label>
          </div>
          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showAchievementModal = false" class="btn btn-outline btn-sm">Cancel</button>
            <button type="submit" :disabled="savingAchievement" class="btn btn-primary btn-sm px-6">
              {{ savingAchievement ? 'Saving...' : 'Save Achievement' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue';
import { Plus, Edit2, Trash2, X, Upload, Monitor, Terminal, Apple, Check, Image as ImageIcon } from 'lucide-vue-next';
import client from '../api/client';

const games = ref([]);
const loading = ref(true);
const showModal = ref(false);
const saving = ref(false);
const editingId = ref(null);
const managingGame = ref(null);
const uploading = ref(null);
const selectedFiles = reactive({});
const uploadStatus = reactive({});
const selectedIcon = ref(null);
const iconRefreshTag = ref(Date.now());
const releaseTab = ref('releases');
const achievements = ref([]);
const showAchievementModal = ref(false);
const savingAchievement = ref(false);
const editingAchievementId = ref(null);
const selectedAchIcon = ref(null);
const achIconRefreshTag = ref(Date.now());

const achForm = reactive({
  title: '',
  description: '',
  hidden: false
});

const form = reactive({
  name: '',
  description: '',
  price: 0,
  publishDate: null,
  trailerVideo: '',
  public: false
});

const fetchCreatedGames = async () => {
  loading.value = true;
  try {
    const res = await client.get('/game/created');
    games.value = res.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const openCreateModal = () => {
  editingId.value = null;
  selectedIcon.value = null;
  form.name = '';
  form.description = '';
  form.price = 0;
  form.publishDate = null;
  form.trailerVideo = '';
  form.public = false;
  showModal.value = true;
};

const openEditModal = (game) => {
  editingId.value = game.id;
  selectedIcon.value = null;
  iconRefreshTag.value = Date.now();
  form.name = game.name;
  form.description = game.description;
  form.price = game.price;
  form.publishDate = game.publishDate ? game.publishDate.split('T')[0] : null;
  form.trailerVideo = game.trailerVideo || '';
  form.public = game.public;
  showModal.value = true;
};

const saveGame = async () => {
  saving.value = true;
  try {
    const data = { ...form };
    if (!data.publishDate) delete data.publishDate;
    if (!data.trailerVideo) data.trailerVideo = null;
    
    let gameId = editingId.value;
    if (editingId.value) {
      await client.patch(`/game/${editingId.value}`, data);
    } else {
      const res = await client.post('/game', data);
      gameId = res.data.id;
    }

    if (selectedIcon.value) {
      const res = await client.post(`/game/${gameId}/icon`);
      const uploadUrl = res.data;
      await fetch(uploadUrl, {
        method: 'PUT',
        body: selectedIcon.value,
        headers: { 'Content-Type': selectedIcon.value.type }
      });
    }

    showModal.value = false;
    await fetchCreatedGames();
  } catch (e) {
    alert(e.response?.data || 'Failed to save game');
  } finally {
    saving.value = false;
  }
};

const handleIconChange = (e) => {
  const file = e.target.files[0];
  if (file) {
    selectedIcon.value = file;
  }
};

const deleteGame = async (game) => {
  if (!confirm(`Are you sure you want to delete "${game.name}"? This action cannot be undone.`)) return;
  try {
    await client.delete(`/game/${game.id}`);
    await fetchCreatedGames();
  } catch (e) {
    alert('Failed to delete game');
  }
};

const openReleaseManager = async (game) => {
  managingGame.value = game;
  selectedFiles.windows = null;
  selectedFiles.linux = null;
  selectedFiles.mac = null;
  uploadStatus.windows = null;
  uploadStatus.linux = null;
  uploadStatus.mac = null;
  releaseTab.value = 'releases';
  await fetchAchievements();
};

const fetchAchievements = async () => {
  if (!managingGame.value) return;
  try {
    const res = await client.get(`/game/${managingGame.value.id}/achievements`);
    achievements.value = res.data;
  } catch (e) {
    console.error(e);
  }
};

const openAchievementModal = (ach = null) => {
  if (ach) {
    editingAchievementId.value = ach.id;
    achForm.title = ach.title;
    achForm.description = ach.description;
    achForm.hidden = ach.hidden;
    achIconRefreshTag.value = Date.now();
  } else {
    editingAchievementId.value = null;
    achForm.title = '';
    achForm.description = '';
    achForm.hidden = false;
  }
  selectedAchIcon.value = null;
  showAchievementModal.value = true;
};

const handleAchIconChange = (e) => {
  selectedAchIcon.value = e.target.files[0];
};

const saveAchievement = async () => {
  savingAchievement.value = true;
  try {
    let achId = editingAchievementId.value;
    if (editingAchievementId.value) {
      await client.patch(`/game/achievement/${editingAchievementId.value}`, achForm);
    } else {
      const res = await client.post(`/game/${managingGame.value.id}/achievements`, achForm);
      achId = res.data.id;
    }

    if (selectedAchIcon.value) {
      const res = await client.post(`/game/achievement/${achId}/icon`);
      const uploadUrl = res.data;
      await fetch(uploadUrl, {
        method: 'PUT',
        body: selectedAchIcon.value,
        headers: { 'Content-Type': selectedAchIcon.value.type }
      });
    }

    showAchievementModal.value = false;
    await fetchAchievements();
  } catch (e) {
    alert('Failed to save achievement');
  } finally {
    savingAchievement.value = false;
  }
};

const deleteAchievement = async (id) => {
  if (!confirm('Are you sure you want to delete this achievement?')) return;
  try {
    await client.delete(`/game/achievement/${id}`);
    await fetchAchievements();
  } catch (e) {
    alert('Failed to delete achievement');
  }
};

const handleFileChange = (platform, event) => {
  selectedFiles[platform] = event.target.files[0];
};

const uploadRelease = async (platform) => {
  const file = selectedFiles[platform];
  if (!file) return;

  uploading.value = platform;
  uploadStatus[platform] = { msg: 'Requesting upload URL...', error: false };

  try {
    // 1. Get pre-signed URL
    const res = await client.post(`/game/${managingGame.value.id}/release/${platform}`);
    const uploadUrl = res.data;

    uploadStatus[platform] = { msg: `Uploading ${file.name}...`, error: false };

    // 2. PUT to S3
    await fetch(uploadUrl, {
      method: 'PUT',
      body: file,
      headers: { 'Content-Type': 'application/octet-stream' }
    });

    uploadStatus[platform] = { msg: 'Upload successful!', error: false };
    selectedFiles[platform] = null;
    
    // Refresh the local game object to show the new build ID
    const refreshRes = await client.get('/game/created');
    games.value = refreshRes.data;
    managingGame.value = games.value.find(g => g.id === managingGame.value.id);
  } catch (e) {
    console.error(e);
    uploadStatus[platform] = { msg: 'Upload failed: ' + e.message, error: true };
  } finally {
    uploading.value = null;
  }
};

onMounted(fetchCreatedGames);
</script>
