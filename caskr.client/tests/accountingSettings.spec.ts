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
})
