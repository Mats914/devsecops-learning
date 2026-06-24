import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    // Proxy: frontend anropar /api → skickas vidare till backend på port 5000
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom', // simulerad webbläsare för komponenttester
    setupFiles: './src/test/setup.ts',
  },
})
