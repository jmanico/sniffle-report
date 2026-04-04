import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'query-vendor': ['@tanstack/react-query', 'axios', 'zod'],
          charts: ['recharts'],
        },
      },
    },
  },
  test: {
    environment: 'happy-dom',
    setupFiles: './src/setupTests.ts',
  },
})
