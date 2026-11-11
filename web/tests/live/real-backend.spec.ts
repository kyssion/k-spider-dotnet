import { expect, test, type Page } from '@playwright/test'

/**
 * Live E2E：真实浏览器访问 admin 托管的构建产物，API 直连真实 admin/PostgreSQL。
 *
 * 凭据只从环境变量读取，不写进仓库；未设置时跳过，避免误用默认账号。
 * 写操作用例要求 Live 库存在 `测试渠道` 与带 `fixture=live-cancel` 的 pushed 任务夹具。
 */
test.describe('真实后端', () => {
  test.skip(
    !process.env.LIVE_USERNAME || !process.env.LIVE_PASSWORD,
    '需要 LIVE_USERNAME / LIVE_PASSWORD 才运行 Live 后端用例',
  )

  test('登录后可以打开核心页面', async ({ page }) => {
    await login(page)

    await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
    await expect(page.getByText('非终态任务')).toBeVisible()

    for (const path of [
      '/api/overview',
      '/api/channels?page=1&pageSize=20',
      '/api/tasks?page=1&pageSize=20',
      '/api/nodes',
      '/api/users',
    ]) {
      const response = await page.request.get(path)
      expect(response.status(), path).toBe(200)
    }

    await page.getByRole('link', { name: '渠道' }).first().click()
    await expect(page.getByRole('heading', { name: '渠道' })).toBeVisible()

    await page.getByRole('link', { name: '任务' }).first().click()
    await expect(page.getByRole('heading', { name: '任务' })).toBeVisible()

    await page.getByRole('link', { name: '节点' }).first().click()
    await expect(page.getByRole('heading', { name: '节点' })).toBeVisible()

    await page.getByRole('link', { name: '账号' }).first().click()
    await expect(page.getByRole('heading', { name: '账号' })).toBeVisible()
  })

  test('渠道暂停与恢复写入真实后端', async ({ page }) => {
    await login(page)
    await page.getByRole('link', { name: '渠道' }).first().click()
    await expect(page.getByRole('heading', { name: '渠道' })).toBeVisible()

    const row = page.getByRole('row').filter({ hasText: '测试渠道' })
    await expect(row).toBeVisible()

    // 兼容上次失败遗留的 paused 状态：先恢复，再验证暂停/恢复两个写路径。
    let paused = false
    try {
      if (await row.getByRole('button', { name: '恢复' }).isVisible()) {
        await row.getByRole('button', { name: '恢复' }).click()
        await expect(row.getByRole('button', { name: '暂停' })).toBeVisible()
      }

      await row.getByRole('button', { name: '暂停' }).click()
      paused = true
      await expect(row.getByRole('button', { name: '恢复' })).toBeVisible()

      await row.getByRole('button', { name: '恢复' }).click()
      paused = false
      await expect(row.getByRole('button', { name: '暂停' })).toBeVisible()
    } finally {
      // 断言失败时尽力恢复，避免把共享测试渠道留在 paused。
      if (paused) {
        await row
          .getByRole('button', { name: '恢复' })
          .click()
          .catch(() => undefined)
      }
    }
  })

  test('取消真实 pushed 任务', async ({ page }) => {
    await login(page)

    const listResponse = await page.request.get('/api/tasks?page=1&pageSize=100&status=pushed')
    expect(listResponse.status()).toBe(200)
    const list = (await listResponse.json()) as {
      items: Array<{ id: number; taskDetailJson: string }>
    }
    const task = list.items.find((item) => item.taskDetailJson.includes('live-cancel'))
    expect(task, 'Live 库需要带 fixture=live-cancel 的 pushed 任务夹具').toBeTruthy()

    await page.goto(`/tasks?status=pushed&task=${task?.id}`)
    await expect(page.getByRole('dialog')).toBeVisible()
    await page.getByRole('button', { name: '确认取消' }).click()
    await expect(page.getByRole('dialog')).toBeHidden()

    await expect
      .poll(async () => {
        const response = await page.request.get('/api/tasks?page=1&pageSize=100&status=cancelled')
        if (response.status() !== 200) {
          return false
        }
        const body = (await response.json()) as { items: Array<{ id: number }> }
        return body.items.some((item) => item.id === task?.id)
      })
      .toBe(true)
  })
})

/** 登录并等待总览加载，供多条 Live 用例复用。 */
async function login(page: Page) {
  await page.goto('/login')
  await page.getByLabel('用户名').fill(process.env.LIVE_USERNAME ?? '')
  await page.getByLabel('密码').fill(process.env.LIVE_PASSWORD ?? '')
  await page.getByRole('button', { name: '登录' }).click()
  await expect(page.getByRole('heading', { name: '总览' })).toBeVisible()
}
