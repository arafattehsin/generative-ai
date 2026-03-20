import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return undefined
          }

          if (id.includes('@mantine')) {
            return 'mantine'
          }

          if (id.includes('@microsoft/signalr')) {
            return 'signalr'
          }

          if (id.includes('@tanstack/react-query')) {
            return 'react-query'
          }

          return undefined
        },
      },
    },
  },
  server: {
    port: 4174,
  },
})
