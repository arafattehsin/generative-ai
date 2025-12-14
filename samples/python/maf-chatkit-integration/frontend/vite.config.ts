import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

const backendTarget = process.env.BACKEND_URL ?? "http://127.0.0.1:8001";

export default defineConfig({
  plugins: [react()],
  server: {
    host: "0.0.0.0",
    port: 5173,
    proxy: {
      "/chatkit": {
        target: backendTarget,
        changeOrigin: true,
      },
      "/upload": {
        target: backendTarget,
        changeOrigin: true,
      },
      "/preview": {
        target: backendTarget,
        changeOrigin: true,
      },
    },
  },
});
