import { defineConfig, devices } from '@playwright/test'

// E2E 测试 : 浏览器跑五个页面的真实渲染与交互 ;
// 业务 API 全部由 tests/helpers/mock-api.ts 用 route 拦截 mock 成夹具——离线确定性 , 可进 CI。
// 真实后端用例在 *.live.spec.ts , 默认跳过 ( LIVE_E2E=1 才执行 , 对齐后端 TestCategory=Live 的分层 )。
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  timeout: 20_000,
  expect: { timeout: 5_000 },
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // 自动起 vite dev server ( /api 走代理 , 但请求在浏览器层已被 mock 拦截 , 不会真的到后端 )
  webServer: {
    command: 'pnpm dev --strictPort',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
