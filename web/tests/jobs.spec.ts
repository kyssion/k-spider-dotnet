import { test, expect } from '@playwright/test'
import { mockApi, mockPostAccepted } from './helpers/mock-api'

const ROUTES = {
  '/api/status/jobs': 'jobs.json',
  '/api/jobs/commands': 'commands.json',
}

test('任务表渲染六个任务与操作按钮', async ({ page }) => {
  await mockApi(page, ROUTES)
  await page.goto('/jobs')
  await expect(page.getByRole('heading', { name: '任务管理' })).toBeVisible()
  // 行定位收在任务表 ( 第一张表 ) : 指令历史表里也含任务名 , 会撞行
  const taskTable = page.getByRole('table').first()
  for (const job of [
    'FlashNewsJob',
    'NewsCheckJob',
    'NewsContentJob',
    'NewsContentOriginJob',
    'NewsListJob',
    'NodeStateJob',
  ])
    await expect(taskTable.getByRole('row', { name: new RegExp(job) })).toBeVisible()
  // 暂停的任务显示"已暂停"徽标 , 操作列给的是"恢复"
  const pausedRow = taskTable.getByRole('row', { name: /NewsContentJob/ })
  await expect(pausedRow.getByText('已暂停')).toBeVisible()
  await expect(pausedRow.getByRole('button', { name: '恢复' })).toBeVisible()
})

test('点击触发发出指令 POST 并展示指令历史', async ({ page }) => {
  await mockApi(page, ROUTES)
  const posted = await mockPostAccepted(page, 'trigger')
  await page.goto('/jobs')
  await page
    .getByRole('table')
    .first()
    .getByRole('row', { name: /FlashNewsJob/ })
    .getByRole('button', { name: '触发' })
    .click()
  await expect(posted).toContainEqual('/api/jobs/FlashNewsJob/trigger')
  // 指令历史表渲染夹具中的两条记录
  await expect(page.getByText('指令历史')).toBeVisible()
  await expect(page.getByRole('cell', { name: '已执行', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '待消费', exact: true })).toBeVisible()
})

test('暂停需要二次确认 , 取消则不发指令', async ({ page }) => {
  await mockApi(page, ROUTES)
  const posted = await mockPostAccepted(page, 'pause')
  await page.goto('/jobs')
  await page
    .getByRole('table')
    .first()
    .getByRole('row', { name: /NewsCheckJob/ })
    .getByRole('button', { name: '暂停' })
    .click()
  // 出现"确认暂停 / 取消" , 点取消后恢复原按钮
  await page.getByRole('button', { name: '取消', exact: true }).click()
  await expect(
    page
      .getByRole('table')
      .first()
      .getByRole('row', { name: /NewsCheckJob/ })
      .getByRole('button', { name: '暂停' }),
  ).toBeVisible()
  expect(posted).toEqual([])
})
