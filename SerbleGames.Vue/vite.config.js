import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => ({
  plugins: [vue()],
  server: {
    port: 3000
  },
  // When building for Electron the app is loaded from file://, so all asset
  // references must be relative rather than absolute.
  base: mode === 'electron' ? './' : '/',
}))
