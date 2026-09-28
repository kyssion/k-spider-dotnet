import { test, expect } from '@playwright/test'
import { mockApi } from './helpers/mock-api'

const ROUTES = { '/api/flash/list': 'flash-list.json' }

test('列表渲染三行与级别徽标', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/flash')
  await expect(page.getByRole('heading', { name: '实时快讯' })).toBeVisible()
  await expect(page.locator('tbody tr')).toHaveCount(3)
  // 级别徽标用单元格定位 : 精确文本会与筛选下拉的 option 撞名
  await expect(page.getByRole('cell', { name: '普通', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '重要', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '重大', exact: true })).toBeVisible()
  await expect(page.getByText('金十重大快讯标题')).toBeVisible()
})

test('点击行打开快讯详情对话框', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/flash')
  await page.getByText('新浪重要快讯标题').click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText('重要快讯的正文内容。')).toBeVisible()
  await expect(dialog.getByText('浦发银行')).toBeVisible()
  await expect(dialog.getByText('原文链接')).toBeVisible()
})

test('级别筛选把查询参数带给接口', async ({ page }) => {
  const mock = await mockApi(page, ROUTES)
  await page.goto('/flash')
  await expect(page.locator('tbody tr')).toHaveCount(3)
  await page.getByLabel('级别').selectOption('3')
  await expect
    .poll(() => mock.requests('/api/flash/list').some(url => url.includes('level=3')))
    .toBe(true)
})
