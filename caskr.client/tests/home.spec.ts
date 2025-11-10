import { expect, test } from '@playwright/test'
import { stubDashboardData } from './support/apiStubs'

test('home page loads dashboard overview content', async ({ page }) => {
  await stubDashboardData(page)

  await page.goto('/')
  await expect(page).toHaveTitle(/Caskr Orders/)
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard' })).toBeVisible()
  await expect(page.getByText('Manage your orders and track progress')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible()
})
