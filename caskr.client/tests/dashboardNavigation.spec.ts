import { expect, test } from '@playwright/test'
import { stubBarrelsData, stubDashboardData } from './support/apiStubs'

test.describe('dashboard navigation', () => {
  test('provides a navigation path to barrels management', async ({ page }) => {
    await stubDashboardData(page)
    await stubBarrelsData(page, [
      { id: 1, sku: 'BRL-1001', orderId: 1, companyId: 1, batchId: 7, rickhouseId: 2 }
    ])

    await page.goto('/')
    await page.getByRole('link', { name: 'Barrels' }).click()

    await expect(page).toHaveURL(/\/barrels$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Barrels' })).toBeVisible()
    await expect(
      page.getByRole('button', { name: 'Open forecasting modal' })
    ).toBeVisible()
    await expect(page.getByRole('cell', { name: 'BRL-1001' })).toBeVisible()
  })
})
