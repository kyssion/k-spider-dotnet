import { test, expect } from '@playwright/test'

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
