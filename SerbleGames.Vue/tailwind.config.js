/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'serble-dark': '#0d1117',
        'serble-card': '#161b22',
        'serble-border': '#30363d',
        'serble-primary': '#238636',
        'serble-primary-hover': '#2ea043',
        'serble-text': '#c9d1d9',
        'serble-text-muted': '#8b949e',
      }
    },
  },
  plugins: [],
}
