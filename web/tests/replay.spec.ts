import { test, expect } from '@playwright/test'
import { mockApi } from './helpers/mock-api'

const ROUTES = {
  '/api/replay/parsers': 'replay-parsers.json',
  '/api/replay/tasks': 'replay-tasks.json',
}

test('重放页渲染解析器配置与任务记录', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/replay')
  await expect(page.getByRole('heading', { name: '数据重放' })).toBeVisible()
  // 四个解析器都进下拉
  const parserSelect = page.getByLabel('解析器')
  for (const code of ['df-article-v1', 'cls-article-v1', 'sina-html-v1', 'wscn-article-v1'])
    await expect(parserSelect.locator(`option[value="${code}"]`)).toHaveCount(1)
  // 任务表 : 完成与运行中两条
  await expect(page.getByRole('cell', { name: '完成', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '运行中', exact: true })).toBeVisible()
  await expect(page.getByText('120 ( 118 / 2 )')).toBeVisible()
})

test('预览命中数展示', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.route('**/api/replay/preview', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: '{"count":42}' }))
  await page.goto('/replay')
  await page.getByRole('button', { name: '预览命中' }).click()
  await expect(page.getByText('命中 42 条 origin')).toBeVisible()
})
