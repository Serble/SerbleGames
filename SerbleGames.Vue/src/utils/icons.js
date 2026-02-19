import { API_BASE } from '../api/client.js';

/**
 * Get the base API URL based on the current window location
 * Works in both dev (localhost:5173) and prod environments
 */
export function getApiBaseUrl() {
  return API_BASE;
  // if (typeof window === 'undefined') {
  //   return 'http://localhost:5240'; // fallback for SSR
  // }
  //
  // const protocol = window.location.protocol;
  // const hostname = window.location.hostname;
  // const port = hostname === 'localhost' ? ':5240' : '';
  //
  // return `${protocol}//${hostname}${port}`;
}

/**
 * Get the game icon URL
 */
export function getGameIconUrl(gameId) {
  return `${getApiBaseUrl()}/game/${gameId}/icon`;
}

/**
 * Get the achievement icon URL
 */
export function getAchievementIconUrl(achievementId) {
  return `${getApiBaseUrl()}/game/achievement/${achievementId}/icon`;
}

/**
 * Default placeholder image URL with lighter black background
 * Using #333333 instead of pure black (#000000)
 */
export const DEFAULT_ICON_URL = 'https://placehold.co/512x512/333333/FFFFFF/png';
