import { defineConfig, devices } from '@playwright/test'

/**
 * Live E2E：真实浏览器 + 真实 admin + 真实 PostgreSQL。
 *
 * 与默认 playwright.config.ts 分开：默认门禁只跑离线 mock 用例；
 * 本配置不启动 webServer，要求 admin 已在 LIVE_BASE_URL（默认 5094）运行。
 */
export default defineConfig({
  testDir: './tests/live',
  fullyParallel: false,
  timeout: 30_000,
  expect: { timeout: 5_000 },
  reporter: [['list']],
  use: {
    baseURL: process.env.LIVE_BASE_URL ?? 'http://127.0.0.1:5094',
    trace: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
