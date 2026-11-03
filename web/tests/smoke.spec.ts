import { test, expect } from '@playwright/test'

test('控制台骨架可渲染', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'KSpider 控制台' })).toBeVisible()
})
