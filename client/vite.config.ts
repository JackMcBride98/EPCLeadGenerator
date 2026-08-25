import react from "@vitejs/plugin-react";
import path from "path";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    strictPort: true,
    proxy: {
      "/api": { target: "https://localhost:5030", secure: false },
    },
  },
  resolve: {
    alias: {
      "@api": path.resolve(import.meta.dirname, "./src/api"),
      "@helpers": path.resolve(import.meta.dirname, "./src/helpers"),
      "@components": path.resolve(import.meta.dirname, "./src/components"),
      "@icons": path.resolve(import.meta.dirname, "./src/icons"),
      "@assets": path.resolve(import.meta.dirname, "./src/assets"),
      "@features": path.resolve(import.meta.dirname, "./src/features"),
    },
  },
});
