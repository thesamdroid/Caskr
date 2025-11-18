import { expect, test } from '@playwright/test'
import { stubQuickBooksAccountingData } from './support/apiStubs'

test.describe('Accounting settings page', () => {
  test('shows connection status and existing mappings', async ({ page }) => {
    await stubQuickBooksAccountingData(page)

    await page.goto('/accounting')

    await expect(page.getByRole('heading', { name: 'Accounting Settings' })).toBeVisible()
    await expect(page.getByText('Realm ID: 1234567890')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Disconnect' })).toBeVisible()

    const cogsRow = page.getByRole('row', { name: /COGS/ })
    await expect(cogsRow.getByRole('combobox')).toHaveValue('acct-1')

    await expect(page.getByRole('button', { name: 'Save Mappings' })).toBeEnabled()
  })

  test('edits sync preferences and verifies connection', async ({ page }) => {
    await stubQuickBooksAccountingData(page, {
      preferences: {
        companyId: 1,
        autoSyncInvoices: false,
        autoSyncCogs: true,
        syncFrequency: 'Daily'
      }
    })

    await page.goto('/accounting')

    const preferencesSection = page.locator('.accounting-preferences')
    await expect(preferencesSection).toBeVisible()

    const invoicesToggle = page.getByLabel('Auto-sync invoices')
    await expect(invoicesToggle).not.toBeChecked()
    await invoicesToggle.check()

    await expect(page.getByLabel('Auto-sync COGS')).toBeChecked()
    await page.getByLabel('Sync frequency').selectOption('Manual')

    await preferencesSection.getByRole('button', { name: 'Save Preferences' }).last().click()
    await expect(page.getByText('Accounting sync preferences saved.')).toBeVisible()

    await preferencesSection.getByRole('button', { name: 'Test Connection' }).first().click()
    await expect(page.getByText('QuickBooks connection verified.')).toBeVisible()
  })
})
