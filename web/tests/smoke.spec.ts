import { test, expect } from '@playwright/test'

/** 门禁冒烟：未登录访问受保护页面必须落到登录页。 */
test('未登录访问控制台会跳到登录页', async ({ page }) => {
  await page.route('**/api/auth/me', (route) =>
    route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'unauthorized' }),
    }),
  )

  await page.goto('/')

  await expect(page.getByRole('heading', { name: '登录控制台' })).toBeVisible()
  await expect(page).toHaveURL(/\/login/)
})
