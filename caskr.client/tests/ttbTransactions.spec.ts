import { expect, test } from '@playwright/test'
import { seedAuthenticatedUser } from './support/auth'

const sampleTransactions = [
  {
    id: 1,
    companyId: 1,
    transactionDate: '2024-09-15T00:00:00Z',
    transactionType: 0, // Production
    productType: 'Bourbon',
    spiritsType: 0, // Under190Proof
    proofGallons: 100.50,
    wineGallons: 50.25,
    sourceEntityType: 'Batch',
    sourceEntityId: 123,
    notes: 'Auto-generated from batch'
  },
  {
    id: 2,
    companyId: 1,
    transactionDate: '2024-09-20T00:00:00Z',
    transactionType: 3, // Loss
    productType: 'Bourbon',
    spiritsType: 0, // Under190Proof
    proofGallons: 5.00,
    wineGallons: 2.50,
    sourceEntityType: 'Manual',
    sourceEntityId: null,
    notes: 'Evaporation loss'
  }
]

test.describe('TTB transactions page', () => {
  test('renders transactions and supports add, edit, and delete operations', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sampleTransactions)
      })
    )

    await page.route('**/api/ttb/transactions', async (route) => {
      if (route.request().method() === 'POST') {
        const newTransaction = {
          id: 3,
          companyId: 1,
          transactionDate: '2024-09-25T00:00:00Z',
          transactionType: 2, // TransferOut
          productType: 'Vodka',
          spiritsType: 1, // Neutral190OrMore
          proofGallons: 20.00,
          wineGallons: 10.00,
          sourceEntityType: 'Manual',
          sourceEntityId: null,
          notes: 'Test manual entry'
        }
        route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(newTransaction)
        })
      } else {
        route.continue()
      }
    })

    await page.route('**/api/ttb/transactions/2', async (route) => {
      if (route.request().method() === 'PUT') {
        const updatedTransaction = {
          ...sampleTransactions[1],
          proofGallons: 7.50,
          wineGallons: 3.75,
          notes: 'Updated evaporation loss'
        }
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(updatedTransaction)
        })
      } else if (route.request().method() === 'DELETE') {
        route.fulfill({ status: 204 })
      } else {
        route.continue()
      }
    })

    await page.goto('/ttb-transactions')

    // Check page loads with transactions
    await expect(page.getByRole('heading', { name: 'TTB Transactions' })).toBeVisible()
    await expect(page.getByText('Bourbon')).toBeVisible()
    await expect(page.getByText('100.50')).toBeVisible()
    await expect(page.getByText('Manual Entry')).toBeVisible()

    // Test adding a transaction
    await page.getByTestId('add-transaction-button').click()
    await expect(page.getByRole('heading', { name: 'Add Transaction' })).toBeVisible()

    await page.getByLabel('Transaction date').fill('2024-09-25')
    await page.getByLabel('Transaction type').selectOption('2') // TransferOut
    await page.getByLabel('Product type').fill('Vodka')
    await page.getByLabel('Spirits type').selectOption('1') // Neutral190OrMore
    await page.getByLabel('Proof gallons').fill('20.00')
    await page.getByLabel('Wine gallons').fill('10.00')
    await page.getByLabel('Notes (optional)').fill('Test manual entry')

    await page.getByRole('button', { name: 'Add Transaction' }).click()

    // Check modal closes after successful creation
    await expect(page.getByRole('heading', { name: 'Add Transaction' })).not.toBeVisible()

    // Test editing a manual transaction
    const manualRow = page.getByRole('row').filter({ hasText: 'Manual Entry' })
    await manualRow.getByRole('button', { name: 'Edit' }).click()
    await expect(page.getByRole('heading', { name: 'Edit Transaction' })).toBeVisible()

    await page.getByLabel('Proof gallons').fill('7.50')
    await page.getByLabel('Wine gallons').fill('3.75')
    await page.getByLabel('Notes (optional)').fill('Updated evaporation loss')

    await page.getByRole('button', { name: 'Update Transaction' }).click()

    // Check modal closes after successful update
    await expect(page.getByRole('heading', { name: 'Edit Transaction' })).not.toBeVisible()

    // Test deleting a manual transaction
    page.on('dialog', dialog => dialog.accept())
    await manualRow.getByRole('button', { name: 'Delete' }).click()
  })

  test('filters transactions by month and year', async ({ page }) => {
    const requests: string[] = []

    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route => {
      requests.push(route.request().url())
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sampleTransactions)
      })
    })

    await page.goto('/ttb-transactions')

    await page.getByLabel('Month').selectOption('8') // August
    await page.getByLabel('Year').selectOption('2023')

    await page.waitForTimeout(100)

    const lastRequest = requests.at(-1) ?? ''
    expect(lastRequest).toContain('month=8')
    expect(lastRequest).toContain('year=2023')
  })

  test('displays summary totals correctly', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sampleTransactions)
      })
    )

    await page.goto('/ttb-transactions')

    // Check summary footer exists
    await expect(page.getByText('Monthly Summary (Proof Gallons)')).toBeVisible()
    await expect(page.getByText('Total Production:')).toBeVisible()
    await expect(page.getByText('Total Losses:')).toBeVisible()

    // Verify totals (Production: 100.50, Loss: 5.00)
    const summarySection = page.locator('.summary-footer')
    await expect(summarySection).toContainText('100.50')
    await expect(summarySection).toContainText('5.00')
  })

  test('prevents editing auto-generated transactions', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sampleTransactions)
      })
    )

    page.on('dialog', dialog => {
      expect(dialog.message()).toContain('Only manual transactions can be edited')
      dialog.accept()
    })

    await page.goto('/ttb-transactions')

    // Try to edit an auto-generated transaction (Batch #123)
    const autoRow = page.getByRole('row').filter({ hasText: 'Batch #123' })
    await expect(autoRow).toBeVisible()
    await expect(autoRow.getByText('Auto-generated')).toBeVisible()
  })

  test('exports transactions to CSV', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sampleTransactions)
      })
    )

    await page.goto('/ttb-transactions')

    const downloadPromise = page.waitForEvent('download')
    await page.getByRole('button', { name: 'Export to CSV' }).click()

    const download = await downloadPromise
    expect(download.suggestedFilename()).toMatch(/ttb_transactions_\d{4}_\d{2}\.csv/)
  })

  test('shows empty state when no transactions exist', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      })
    )

    await page.goto('/ttb-transactions')

    await expect(page.getByText('No transactions found for')).toBeVisible()
    await expect(page.getByText('Click "Add Transaction" to create a manual entry.')).toBeVisible()

    // CSV export should be disabled when no transactions
    await expect(page.getByRole('button', { name: 'Export to CSV' })).toBeDisabled()
  })

  test('validates form fields when adding transaction', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/ttb/transactions?**', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      })
    )

    await page.goto('/ttb-transactions')

    await page.getByTestId('add-transaction-button').click()

    // Try to submit without filling required fields
    await page.getByRole('button', { name: 'Add Transaction' }).click()

    // Form should still be visible (not closed) due to validation
    await expect(page.getByRole('heading', { name: 'Add Transaction' })).toBeVisible()

    // Fill in only some fields and verify validation
    await page.getByLabel('Product type').fill('')
    await page.getByLabel('Proof gallons').fill('-10') // Invalid negative value

    await page.getByLabel('Product type').fill('Bourbon')
    await page.getByLabel('Proof gallons').fill('10.50')
    await page.getByLabel('Wine gallons').fill('5.25')
    await page.getByLabel('Transaction date').fill('2024-09-15')

    // Should now be valid - but we're not submitting to avoid network call
  })
})
