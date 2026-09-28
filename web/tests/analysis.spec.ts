import { test, expect } from '@playwright/test'
import { mockApi } from './helpers/mock-api'

const ROUTES = {
  '/api/analysis/volume': 'volume.json',
  '/api/analysis/distribute': 'distribute.json',
  '/api/analysis/keywords': 'keywords.json',
}

test('入库量趋势渲染双折线', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/analysis')
  await expect(page.getByText('入库量趋势', { exact: true })).toBeVisible()
  await expect(page.locator('.recharts-line')).toHaveCount(2)
})

test('分布卡片渲染柱状图与饼图', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/analysis')
  await expect(page.getByText('网页新闻 · 按来源')).toBeVisible()
  await expect(page.getByText('快讯 · 按来源')).toBeVisible()
  await expect(page.getByText('网页新闻 · 按栏目号')).toBeVisible()
  await expect(page.getByText('快讯 · 按重要度')).toBeVisible()
  // 柱状图 3 张 + 饼图 1 张 , 饼图分片数等于源数量
  await expect(page.locator('.recharts-bar')).toHaveCount(3)
  await expect(page.locator('.recharts-pie-sector')).toHaveCount(4)
})

test('关键词 Top 渲染词与计数', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/analysis')
  await expect(page.getByText('关键词 Top 50')).toBeVisible()
  // 徽标内是 "词 + 计数 span" , 用子串匹配 ( 本页无其它同名文本 )
  await expect(page.getByText('A股')).toBeVisible()
  await expect(page.getByText('宏观')).toBeVisible()
})

test('切换粒度把 granularity 参数带给接口', async ({ page }) => {
  const mock = await mockApi(page, ROUTES)
  await page.goto('/analysis')
  await expect(page.locator('.recharts-line')).toHaveCount(2)
  await page.getByLabel('粒度').selectOption('hour')
  await expect
    .poll(() =>
      mock.requests('/api/analysis/volume').some(url => url.includes('granularity=hour')))
    .toBe(true)
})
