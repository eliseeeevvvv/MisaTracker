
import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = 'misatrackerr.client';
const certFilePath = path.join(
    baseFolder,
    `${certificateName}.pem`
);
const keyFilePath = path.join(
    baseFolder,
    `${certificateName}.key`
);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (
        child_process.spawnSync(
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
        ).status !== 0
    ) {
        throw new Error('Could not create certificate.');
    }
}

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(';')[0]
        : 'https://localhost:7208';

export default defineConfig({
    plugins: [
        plugin(),

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

        port: parseInt(env.DEV_SERVER_PORT || '64106'),

        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        },
    },
});
