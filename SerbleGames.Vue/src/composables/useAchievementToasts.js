import { reactive } from 'vue';

const TOAST_LIFETIME_MS = 5000;

let _seq = 0;

/**
 * Module-level queue so every component instance shares the same list.
 * @type {{ id: number, gameId: string, achievementId: string,
 *          achievementTitle: string|null, achievementDescription: string|null }[]}
 */
const toasts = reactive([]);

/**
 * Composable that exposes the global achievement toast queue and an
 * addToast() helper.  Import in any component that needs to read or push.
 */
export function useAchievementToasts() {
  /**
   * @param {{ gameId: string, achievementId: string,
   *           achievementTitle?: string|null,
   *           achievementDescription?: string|null }} data
   */
  function addToast(data) {
    const id = ++_seq;
    toasts.push({ id, ...data });
    setTimeout(() => {
      const idx = toasts.findIndex((t) => t.id === id);
      if (idx !== -1) toasts.splice(idx, 1);
    }, TOAST_LIFETIME_MS + 400); // extra 400 ms for leave animation
  }

  return { toasts, addToast, TOAST_LIFETIME_MS };
}
