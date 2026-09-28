import { test, expect } from '@playwright/test'
import { fileURLToPath } from 'node:url'
import { mockApi } from './helpers/mock-api'

const ROUTES = {
  '/api/status/jobs': 'jobs.json',
  '/api/status/pipeline': 'pipeline.json',
  '/api/status/flash-lag': 'flash-lag.json',
  '/api/status/nodes': 'nodes.json',
}

test.beforeEach(async ({ page }) => {
  await mockApi(page, ROUTES)
})

test('任务卡片渲染六个任务与执行摘要', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
  await expect(page.getByText('FlashNewsJob')).toBeVisible()
  await expect(page.getByText('写入快讯 2')).toBeVisible()
  await expect(page.getByText('新增列表 3')).toBeVisible()
  await expect(page.getByText('解析 356/356 , 失败 0 , 退回下载 0')).toBeVisible()
  // 暂停的任务带"已暂停"徽标 , 失败任务带连续失败计数
  await expect(page.getByText('已暂停')).toBeVisible()
  await expect(page.getByText('连续失败 2 次')).toBeVisible()
})

test('管线积压按源展示状态计数与最老待处理', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: '网页管线积压' })).toBeVisible()
  await expect(page.getByText('待下载 17222')).toBeVisible()
  await expect(page.getByText('已解析 780')).toBeVisible()
  await expect(page.getByText('最老待处理 2026-09-20 16:50:35')).toBeVisible()
})

test('快讯实时性列出四个源与滞后', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: '快讯实时性' })).toBeVisible()
  // 四个源各一行 ( 无数据的源显示占位 )
  await expect(page.getByText('最新一条')).toHaveCount(4)
  await expect(page.getByText('30 秒前')).toBeVisible()
  await expect(page.getByText('无数据')).toBeVisible()
})

test('在线节点展示反爬验证冷却告警', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('在线 ·').first()).toBeVisible()
  // payload 里的 verifyBlocked 渲染成告警条
  await expect(page.getByText(/www\.example\.com/)).toBeVisible()
  await expect(page.getByText(/SliderCaptcha/)).toBeVisible()
})

test('快照过旧的节点显示失联', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.route('**/api/status/nodes', route =>
    route.fulfill({
      path: fileURLToPath(new URL('./fixtures/nodes-stale.json', import.meta.url)),
    }))
  await page.goto('/')
  await expect(page.getByText(/失联/)).toBeVisible()
  await expect(page.getByText(/接口异常 : \[2 ClsMedia\]/)).toBeVisible()
})
