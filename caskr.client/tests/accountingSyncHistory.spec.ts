import { expect, test } from '@playwright/test'
import { stubQuickBooksSyncLogs } from './support/apiStubs'

test.describe('Accounting sync history page', () => {
  test('renders sync log table with filters and actions', async ({ page }) => {
    await stubQuickBooksSyncLogs(page)

    await page.goto('/accounting/sync-history')

    await expect(page.getByRole('heading', { name: 'Accounting Sync History' })).toBeVisible()
    await expect(page.getByLabel('Status')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Export CSV' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Retry Failed Syncs' })).toBeEnabled()

    await expect(page.getByText('Customer missing in QuickBooks.')).toBeVisible()
    await expect(page.getByRole('link', { name: 'View in QuickBooks' })).toBeVisible()
  })

  test('retries an invoice sync from the history table', async ({ page }) => {
    const logs = [
      {
        id: '200',
        syncedAt: new Date('2024-05-10T10:00:00Z').toISOString(),
        entityType: 'Invoice',
        entityId: 202,
        status: 'Failed',
        qboId: null,
        errorMessage: 'Line items missing.'
      }
    ]

    await stubQuickBooksSyncLogs(page, { logs })

    await page.goto('/accounting/sync-history')

    const failedRow = page.getByRole('row', { name: /Invoice/ })
    await failedRow.getByRole('button', { name: 'Retry' }).click()
    await expect(page.getByText('Retry started for invoice 202.')).toBeVisible()
  })
})
