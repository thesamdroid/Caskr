import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const useHttps = env.VITE_USE_HTTPS !== 'false';
const disableProxy = env.VITE_DISABLE_PROXY === 'true';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "caskr.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

let httpsEnabled = useHttps;

if (useHttps) {
    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        const result = child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit', });

        if (result.status !== 0) {
            console.warn("Could not create certificate. Falling back to HTTP.");
            httpsEnabled = false;
        }
    }
}

const target = env.VITE_ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.VITE_ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:32769';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: (() => {
        const serverConfig: Record<string, unknown> = {
            port: 51844,
            ...(httpsEnabled && fs.existsSync(keyFilePath) && fs.existsSync(certFilePath) ? {
                https: {
                    key: fs.readFileSync(keyFilePath),
                    cert: fs.readFileSync(certFilePath),
                }
            } : {})
        };

        if (!disableProxy) {
            serverConfig.proxy = {
                '^/api/.*': {
                    target,
                    secure: false
                }
            };
        }

        return serverConfig;
    })()
})
