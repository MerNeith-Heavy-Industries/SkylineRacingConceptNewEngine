import { defineConfig } from "vite";
import preact from "@preact/preset-vite";
import { resolve } from "path";

// Single-page app with hash-based client router.
// All phases share one index.html entry; navigation uses #/main-menu etc.
export default defineConfig({
  plugins: [preact({
    devToolsEnabled: true,
    devtoolsInProd: true,
  })],
  root: "src",
  base: "./",
  resolve: {
    alias: {
      "@shared": resolve(__dirname, "src/shared"),
    },
  },
  build: {
    outDir: "../dist",
    emptyOutDir: true,
    rollupOptions: {
      input: resolve(__dirname, "src/index.html"),
    },
    target: "es2022",
    minify: "esbuild",
    sourcemap: false,
  },
  server: {
    port: 5173,
    strictPort: true,
  },
});
