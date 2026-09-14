import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  base: "/workbench/",
  build: {
    outDir: process.env.VITE_DESKTOP_MODE === "1"
      ? "../desktop-dist/static-workbench"
      : "../../ops-workbench-runtime/static-workbench",
    emptyOutDir: true,
  },
});
