import { test, expect } from '@playwright/test'

// 真实后端连通性 : 需要先启动 k-spider-web ( 5800 ) , 用 LIVE_E2E=1 pnpm test:e2e:live 执行 ;
// 默认跳过 ( 与后端 Live 测试分类同款分层 : 门禁保持离线确定 )。
test.skip(!process.env.LIVE_E2E, '需要真实后端 : 先 dotnet run --project src/k-spider-web 再 LIVE_E2E=1 执行')

test.use({ baseURL: 'http://localhost:5800' })

test('@live 健康探针可访问', async ({ request }) => {
  const res = await request.get('/api/health')
  expect(res.ok()).toBeTruthy()
  expect((await res.json()).ok).toBe(true)
})

test('@live 总览页能渲染任务状态', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
  await expect(page.getByText('任务状态')).toBeVisible()
})
