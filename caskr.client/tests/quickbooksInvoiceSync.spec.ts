import { test, expect } from '@playwright/test'

test.describe('orders quickbooks sync ui', () => {
  test('shows synced invoice link when QuickBooks connection is active', async ({ page }) => {
    const invoiceId = 555
    const order = {
      id: 1,
      name: 'QuickBooks Order',
      statusId: 1,
      companyId: 99,
      invoiceId,
      ownerId: 1,
      spiritTypeId: 1,
      quantity: 1,
      mashBillId: 1
    }

    await page.route('**/api/status', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 1, name: 'New', statusTasks: [] }])
      })
    )

    await page.route('**/api/orders', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([order])
        })
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' })
      }
    })

    await page.route('**/api/orders/1/tasks', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
    )

    await page.route('**/api/accounting/quickbooks/status**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ connected: true, realmId: 'test' })
      })
    )

    await page.route('**/api/accounting/quickbooks/invoice-status**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          invoiceId,
          status: 'Success',
          qboInvoiceId: 'INV-12345',
          errorMessage: null,
          lastSyncedAt: new Date().toISOString()
        })
      })
    )

    await page.goto('/orders')
    await page.getByText('QuickBooks Order').click()

    const modal = page.locator('.order-actions-modal')
    await expect(modal).toBeVisible()
    await expect(modal.getByText('QuickBooks Sync')).toBeVisible()
    await expect(modal.locator('.sync-status-badge')).toHaveText('Synced')

    const invoiceLink = modal.getByRole('link', { name: 'INV-12345' })
    await expect(invoiceLink).toHaveAttribute('href', 'https://app.qbo.intuit.com/app/invoice?txnId=INV-12345')
  })
})
