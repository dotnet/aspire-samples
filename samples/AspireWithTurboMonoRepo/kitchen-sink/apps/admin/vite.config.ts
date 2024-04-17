import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  //
  // BEGIN added to support Aspire hosting
  //
  server: {
    port: Number(process.env.PORT ?? 3001),
  },
  preview: {
    port: Number(process.env.PORT ?? 3001),
  },
  //
  // END added to support Aspire hosting
  //
});
