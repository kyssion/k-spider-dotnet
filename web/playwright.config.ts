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
  // 构建产物 + preview 伺服 : 比 dev server 启动快且稳定 , 且测的就是生产构建形态。
  // 命令直接调 vite 二进制 , 不经 pnpm/shell 中间层——CI ( linux ) 上 pnpm→shell→vite 的进程树
  // 在 playwright 收尾杀 webServer 时收不干净 , stdio 管道悬空导致测试全过后 CLI 永不退出
  // ( 实测挂满 6 小时被取消 ; mac 上进程组终止行为不同故本地不复现 )。
  // dist 需先构建 : verify.sh / CI 都先跑 build ; 本地单独跑 test:e2e 前先 pnpm build。
  webServer: {
    command: 'node_modules/.bin/vite preview --strictPort --port 5899 --host 127.0.0.1',
    url: 'http://127.0.0.1:5899',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
