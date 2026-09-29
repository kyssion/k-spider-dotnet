import { test, expect } from '@playwright/test'
import { mockApi } from './helpers/mock-api'

const ROUTES = {
  '/api/news/list': 'news-list.json',
  '/api/news/detail': 'news-detail.json',
}

test('列表渲染三行与状态徽标', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/news')
  await expect(page.getByRole('heading', { name: '网页新闻' })).toBeVisible()
  await expect(page.locator('tbody tr')).toHaveCount(3)
  await expect(page.getByText('东方财富测试新闻标题一')).toBeVisible()
  // 状态徽标用单元格定位 : 精确文本会与筛选下拉的 option 撞名
  await expect(page.getByRole('cell', { name: '待下载', exact: true })).toBeVisible()
  // 夹具第 2 条为已下载 + 付费 ( 两徽标同格 , 格名合并为 "已下载 付费" )
  await expect(page.getByRole('cell', { name: '已下载 付费' })).toBeVisible()
  await expect(page.getByRole('cell', { name: '已解析', exact: true })).toBeVisible()
  await expect(page.getByText('共 3 条 · 第 1 / 1 页')).toBeVisible()
})

test('点击行打开详情对话框 ( 含正文 / 关键词 / 图片 )', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/news')
  await page.getByText('东方财富测试新闻标题一').click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText('正文第一段内容。')).toBeVisible()
  await expect(dialog.getByText('A股', { exact: true })).toBeVisible()
  await expect(dialog.getByRole('img')).toHaveCount(1)
  await expect(dialog.getByText('https://example.com/news/n1')).toBeVisible()
  // Esc 关闭
  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
})

test('来源筛选把查询参数带给接口', async ({ page }) => {
  const mock = await mockApi(page, ROUTES)
  await page.goto('/news')
  await expect(page.locator('tbody tr')).toHaveCount(3)
  await page.getByLabel('来源').selectOption('2')
  // 筛选变化触发重新请求 , 查询串应带上 source=2
  await expect
    .poll(() => mock.requests('/api/news/list').some(url => url.includes('source=2')))
    .toBe(true)
})
