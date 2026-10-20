import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Proxy API calls to the Python app service
      '/api': {
        target: process.env.APP_HTTPS || process.env.APP_HTTP,
        changeOrigin: true,
        secure: false
      }
    }
  },
  define: {
    // Pass Keycloak config to the frontend at build/dev time
    __KEYCLOAK_URL__: JSON.stringify(process.env.services__keycloak__https__0 || ''),
    __KEYCLOAK_REALM__: JSON.stringify(process.env.KEYCLOAK_REALM || 'aspirekeycloaksample'),
    __KEYCLOAK_CLIENT_ID__: JSON.stringify(process.env.KEYCLOAK_CLIENT_ID || 'keycloak.web.frontend'),
  }
});
