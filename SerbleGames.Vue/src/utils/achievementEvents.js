import { reactive } from 'vue';

/**
 * Module-level reactive object that records the most recently granted
 * achievement.  Any Vue component can watch this to react to grants without
 * prop drilling or a full state-management library.
 *
 * Updated by App.vue when it receives an `achievement-granted` IPC event.
 */
export const lastGrantedAchievement = reactive({
  /** @type {string|null} */
  gameId: null,
  /** @type {string|null} */
  achievementId: null,
  /** Ever-increasing counter used as the watch trigger. */
  seq: 0,
});
