import { defineConfig } from "vite";
import { createVuePlugin } from 'vite-plugin-vue2';

export default defineConfig({
    base: '', // Relative base path for flexible dev serving
    plugins: [
        createVuePlugin()
    ],
    server: {
        port: 5173,
        proxy: {
            // Proxy API requests during AI Vite debugging sessions to the ASP.NET Core host process
            '/api': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false
            }
        }
    },
    build: {
        rollupOptions: {
            external: [
                /^https:\/\//
            ]
        }
    }
});