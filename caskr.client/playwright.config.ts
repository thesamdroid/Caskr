import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  webServer: {
    command: 'npm run dev -- --port 51844',
    url: 'http://localhost:51844',
    timeout: 120 * 1000,
    reuseExistingServer: !process.env.CI,
    env: {
      VITE_USE_HTTPS: 'false',
      VITE_DISABLE_PROXY: 'true',
    }
  },
  use: {
    baseURL: 'http://localhost:51844',
    headless: true,
  },
  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'], channel: 'chrome', launchOptions: { args: ['--no-sandbox'] } },
    },
  ],
});
