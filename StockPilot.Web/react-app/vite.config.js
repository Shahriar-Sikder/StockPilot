import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = fileURLToPath(new URL('..', import.meta.url));
const frontendRoot = fileURLToPath(new URL('.', import.meta.url));
const dashboardView = path.join(webRoot, 'App', 'tenant', 'views', 'dashboard');

export default defineConfig({
  root: dashboardView,
  plugins: [react()],
  base: '/react/',
  resolve: {
    dedupe: ['react', 'react-dom'],
    alias: [
      { find: 'react', replacement: path.join(frontendRoot, 'node_modules', 'react') },
      { find: 'react-dom', replacement: path.join(frontendRoot, 'node_modules', 'react-dom') }
    ]
  },
  build: {
    outDir: path.join(webRoot, 'react'),
    emptyOutDir: true,
    assetsDir: 'assets'
  }
});
