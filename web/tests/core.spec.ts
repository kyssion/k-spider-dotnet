import { test, expect, type Page } from '@playwright/test'

const ME = {
  id: 1,
  username: 'admin',
  displayName: '管理员',
  csrfToken: 'csrf-token',
}

const OVERVIEW = {
  generatedAt: '2026-11-10T12:00:00Z',
  channels: { total: 2, active: 1, paused: 1, retired: 0 },
  tasks: {
    total: 3,
    byStatus: {
      pushing: 0,
      push_failed: 1,
      pushed: 0,
      running: 1,
      run_failed: 0,
      success: 1,
      abandoned: 0,
      cancelled: 0,
    },
  },
  nodes: { total: 1, online: 1, offline: 0 },
  recentFailures: [
    {
      taskId: 7,
      channelId: 1,
      nodeId: '11111111-1111-1111-1111-111111111111',
      status: 'push_failed',
      failCount: 1,
      lastError: 'network down',
      updatedAt: '2026-11-10T11:59:00Z',
    },
  ],
}

const CHANNELS = {
  items: [
    {
      id: 1,
      categoryId: 1,
      channelName: '测试渠道',
      sourceAdapter: 'test.source',
      detailAdapter: 'test.detail',
      writeMode: 'overwrite',
      retentionDays: 30,
      status: 'active',
      scheduleJson: '{"enabled":true,"interval_seconds":60}',
      extraJson: '{}',
      createTime: '2026-11-01T00:00:00Z',
      updateTime: '2026-11-10T11:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  total: 1,
}

const TASKS = {
  items: [
    {
      id: 1,
      nodeId: '11111111-1111-1111-1111-111111111111',
      taskType: 'channel_round',
      taskDetailJson: '{"channel_id":1}',
      status: 'running',
      extraJson: '{"fail_count":0}',
      createTime: '2026-11-10T11:00:00Z',
      updateTime: '2026-11-10T11:01:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  total: 1,
}

async function mockMe(page: Page) {
  await page.route('**/api/auth/me', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(ME),
    }),
  )
}

test('已登录用户可以打开总览', async ({ page }) => {
  await mockMe(page)
  await page.route('**/api/overview', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(OVERVIEW),
    }),
  )

  await page.goto('/')

  await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
  await expect(page.getByText('非终态任务')).toBeVisible()
  await expect(page.getByText('最近失败任务')).toBeVisible()
})

test('渠道列表展示并可以暂停渠道', async ({ page }) => {
  await mockMe(page)
  let paused = false
  await page.route(/\/api\/channels(\?.*)?$/, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(CHANNELS),
    }),
  )
  await page.route(/\/api\/channels\/1\/pause$/, (route) => {
    paused = true
    return route.fulfill({ status: 204 })
  })

  await page.goto('/channels')

  await expect(page.getByRole('heading', { name: '渠道' })).toBeVisible()
  await expect(page.getByText('测试渠道')).toBeVisible()
  await page.getByRole('button', { name: '暂停' }).click()
  await expect.poll(() => paused).toBe(true)
})

test('取消任务需要二次确认', async ({ page }) => {
  await mockMe(page)
  let cancelled = false
  await page.route(/\/api\/tasks(\?.*)?$/, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(TASKS),
    }),
  )
  await page.route(/\/api\/tasks\/1\/cancel(\?.*)?$/, (route) => {
    cancelled = true
    return route.fulfill({ status: 204 })
  })

  await page.goto('/tasks')

  await expect(page.getByRole('heading', { name: '任务' })).toBeVisible()
  await page.getByRole('button', { name: '取消' }).click()
  await expect(page.getByRole('dialog', { name: '任务 #1' })).toBeVisible()
  await page.getByRole('button', { name: '确认取消' }).click()
  await expect.poll(() => cancelled).toBe(true)
})

test('登录成功后回到总览', async ({ page }) => {
  let loggedIn = false
  await page.route('**/api/auth/me', (route) =>
    route.fulfill({
      status: loggedIn ? 200 : 401,
      contentType: 'application/json',
      body: JSON.stringify(loggedIn ? ME : { error: 'unauthorized' }),
    }),
  )
  await page.route('**/api/auth/login', (route) => {
    loggedIn = true
    return route.fulfill({ status: 204 })
  })
  await page.route('**/api/overview', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(OVERVIEW),
    }),
  )

  await page.goto('/login')
  await page.getByLabel('用户名').fill('admin')
  await page.getByLabel('密码').fill('admin')
  await page.getByRole('button', { name: '登录' }).click()

  await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
})

test('节点页展示节点状态', async ({ page }) => {
  await mockMe(page)
  await page.route('**/api/nodes', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          nodeId: '11111111-1111-1111-1111-111111111111',
          nodeType: 'crawler',
          status: 'online',
          deviceJson: '{"host":"node-1"}',
          extraJson: '{}',
          createTime: '2026-11-10T10:00:00Z',
          updateTime: '2026-11-10T11:00:00Z',
        },
      ]),
    }),
  )

  await page.goto('/nodes')

  await expect(page.getByRole('heading', { name: '节点' })).toBeVisible()
  await expect(page.getByText('在线', { exact: true })).toBeVisible()
})

test('账号页展示账号并打开新建对话框', async ({ page }) => {
  await mockMe(page)
  await page.route('**/api/users', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 1,
          username: 'admin',
          displayName: '管理员',
          status: 'active',
          createTime: '2026-11-01T00:00:00Z',
        },
      ]),
    }),
  )

  await page.goto('/settings/users')

  await expect(page.getByRole('heading', { name: '账号' })).toBeVisible()
  await expect(page.getByText('admin')).toBeVisible()
  await page.getByRole('button', { name: '新建账号' }).click()
  await expect(page.getByRole('dialog', { name: '新建账号' })).toBeVisible()
})

test('渠道详情展示适配器与状态', async ({ page }) => {
  await mockMe(page)
  await page.route('**/api/channels/1', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(CHANNELS.items[0]),
    }),
  )

  await page.goto('/channels/1')

  await expect(page.getByRole('heading', { name: '测试渠道' })).toBeVisible()
  await expect(page.getByText('test.source')).toBeVisible()
  await expect(page.getByText('启用')).toBeVisible()
})
