<template>
  <!-- Teleport to <body> so nothing in the component tree clips or covers it -->
  <Teleport to="body">
    <div
      aria-live="polite"
      aria-label="Achievement notifications"
      class="fixed bottom-5 right-5 z-[9999] flex flex-col-reverse gap-3 pointer-events-none"
    >
      <TransitionGroup name="achievement-toast">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="pointer-events-auto w-80 rounded-xl overflow-hidden shadow-2xl
                 bg-serble-card border border-serble-border"
        >
          <!-- Gold header -->
          <div class="flex items-center gap-2 px-4 py-2.5
                      bg-yellow-500/10 border-b border-yellow-500/20">
            <Trophy class="w-4 h-4 text-yellow-400 shrink-0" />
            <span class="text-xs font-bold text-yellow-300 uppercase tracking-widest">
              Achievement Unlocked
            </span>
          </div>

          <!-- Body -->
          <div class="flex items-center gap-3 p-3">
            <!-- Icon -->
            <div class="w-14 h-14 rounded-lg overflow-hidden shrink-0
                        border border-serble-border bg-serble-dark">
              <img
                :src="getAchievementIconUrl(toast.achievementId)"
                class="w-full h-full object-cover"
                :onError="(e) => e.target.src = DEFAULT_ICON_URL"
                alt=""
              />
            </div>

            <!-- Text -->
            <div class="min-w-0 flex-1">
              <p class="font-bold text-sm text-white leading-tight truncate">
                {{ toast.achievementTitle || toast.achievementId }}
              </p>
              <p
                v-if="toast.achievementDescription"
                class="text-xs text-serble-text-muted line-clamp-2 mt-0.5 leading-snug"
              >
                {{ toast.achievementDescription }}
              </p>
            </div>
          </div>

          <!-- Auto-dismiss progress bar -->
          <div class="h-0.5 w-full bg-serble-border/20">
            <div class="h-full bg-yellow-500/70 toast-progress" />
          </div>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup>
import { Trophy } from 'lucide-vue-next';
import { useAchievementToasts } from '../composables/useAchievementToasts.js';
import { getAchievementIconUrl, DEFAULT_ICON_URL } from '../utils/icons.js';

const { toasts, TOAST_LIFETIME_MS } = useAchievementToasts();
// Used by v-bind() in the <style> block to set the animation duration.
// eslint-disable-next-line no-unused-vars
const progressDuration = `${TOAST_LIFETIME_MS}ms`;
</script>

<style scoped>
/* Slide in from the right, slide back out */
.achievement-toast-enter-active {
  animation: toast-in 0.35s cubic-bezier(0.22, 1, 0.36, 1) both;
}
.achievement-toast-leave-active {
  animation: toast-out 0.3s ease-in both;
}
.achievement-toast-move {
  transition: transform 0.3s ease;
}

@keyframes toast-in {
  from { transform: translateX(110%); opacity: 0; }
  to   { transform: translateX(0);    opacity: 1; }
}
@keyframes toast-out {
  from { transform: translateX(0);    opacity: 1; }
  to   { transform: translateX(110%); opacity: 0; }
}

/* Shrinks from 100 → 0 over TOAST_LIFETIME_MS.
   The duration is set via CSS variable injected per-toast in the template,
   but since all toasts share the same lifetime we hardcode it here. */
.toast-progress {
  animation: deplete v-bind(progressDuration) linear forwards;
}
@keyframes deplete {
  from { width: 100%; }
  to   { width: 0%;   }
}
</style>
