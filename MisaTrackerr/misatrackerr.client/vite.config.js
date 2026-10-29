
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

import fs from 'node:fs';
import path from 'node:path';
import child_process from 'node:child_process';
import { env } from 'node:process';

const isDevelopment = env.NODE_ENV !== 'production';

const baseFolder =
    env.APPDATA
        ? path.join(env.APPDATA, 'ASP.NET', 'https')
        : path.join(env.HOME || '', '.aspnet', 'https');

const certificateName = 'misatrackerr.client';
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

let httpsConfig = undefined;

if (isDevelopment) {
    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        const result = child_process.spawnSync(
            'dotnet',
            [
                'dev-certs',
                'https',
                '--export-path',
                certFilePath,
                '--format',
                'Pem',
                '--no-password',
            ],
            { stdio: 'inherit' }
        );

        if (result.status !== 0) {
            throw new Error(
                'Не удалось создать HTTPS-сертификат для локальной разработки.'
            );
        }
    }

    httpsConfig = {
        key: fs.readFileSync(keyFilePath),
        cert: fs.readFileSync(certFilePath),
    };
}

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(';')[0]
        : 'https://localhost:7208';

export default defineConfig({
    base: '/',
    plugins: [
        react(),

        VitePWA({
            strategies: 'injectManifest',
            srcDir: 'src',
            filename: 'sw.js',
            registerType: 'autoUpdate',

            devOptions: {
                enabled: true,
                type: 'module',
            },

            manifest: {
                id: '/',
                name: 'MisaTracker',
                short_name: 'MisaTracker',
                description: 'Трекер учёбы, заданий и дедлайнов',
                start_url: '/',
                scope: '/',
                display: 'standalone',
                orientation: 'portrait',
                background_color: '#FFFDF7',
                theme_color: '#E7EEDC',
                icons: [
                    {
                        src: '/icons/icon-192.png',
                        sizes: '192x192',
                        type: 'image/png',
                    },
                    {
                        src: '/icons/icon-512.png',
                        sizes: '512x512',
                        type: 'image/png',
                    },
                ],
            },
        }),
    ],

    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },

    server: {
        host: '0.0.0.0',
        port: parseInt(env.DEV_SERVER_PORT || '64106', 10),

        ...(httpsConfig ? { https: httpsConfig } : {}),

        proxy: {
            '/api': {
                target,
                changeOrigin: true,
                secure: false,
            },
            '^/weatherforecast': {
                target,
                secure: false,
            },
        },
    },
});
