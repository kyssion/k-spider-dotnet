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
    // 显式 IP : CI 上 localhost 的 IPv4/IPv6 解析不一致会让 webServer 就绪探测永远不通过
    baseURL: 'http://127.0.0.1:5899',
    trace: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // 构建产物 + preview 伺服 : 比 dev server 启动快且稳定 ( dev 在 CI 上冷启动慢、偶发就绪探测挂起 ) ,
  // 且测的就是生产构建形态 ; 端口用专用的 5899 而不是 vite 默认 5173 ( 会被本机其它 vite 项目抢占 ,
  // 本地 reuseExistingServer 会误复用 ); 命令自带 build , 单独跑 test:e2e 无需先手动构建
  webServer: {
    command: 'pnpm build && pnpm preview --strictPort --port 5899 --host 127.0.0.1',
    url: 'http://127.0.0.1:5899',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
