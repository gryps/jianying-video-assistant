import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  base: "/workbench/",
  build: {
    outDir: mode === "desktop"
      ? "../desktop-dist/static-workbench"
      : "../../ops-workbench-runtime/static-workbench",
    emptyOutDir: true,
  },
}));
