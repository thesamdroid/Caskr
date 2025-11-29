import { expect, test } from '@playwright/test'

const sampleReports = [
  { id: 1, reportMonth: 10, reportYear: 2024, status: 'Draft', generatedAt: '2024-11-05T00:00:00Z' },
  { id: 2, reportMonth: 9, reportYear: 2024, status: 'Submitted', generatedAt: '2024-10-03T00:00:00Z' }
]

test.describe('TTB reports page', () => {
  test('renders reports and supports preview, download, and generation flows', async ({ page }) => {
    await page.route('**/api/ttb/reports?**', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sampleReports) })
    )

    await page.route('**/api/ttb/reports/generate', route =>
      route.fulfill({ status: 200, contentType: 'application/pdf', body: 'PDF-CONTENT' })
    )

    await page.route('**/api/ttb/reports/**/download', route =>
      route.fulfill({ status: 200, contentType: 'application/pdf', body: 'PDF-CONTENT' })
    )

    await page.goto('/ttb-reports')

    await expect(page.getByRole('heading', { name: 'TTB Monthly Reports (Form 5110.28)' })).toBeVisible()
    const octoberRow = page.getByRole('row', { name: /October 2024/ })
    await expect(octoberRow).toBeVisible()
    await expect(octoberRow.getByText('Draft')).toBeVisible()

    await page.getByTestId('generate-ttb-report-button').click()
    await page.getByLabel('Reporting month').selectOption('11')
    await page.getByLabel('Reporting year').selectOption('2024')
    await page.getByRole('button', { name: 'Generate report' }).click()
    await expect(page.getByText('TTB report generated. Download started for the new draft.')).toBeVisible()

    await page.getByRole('button', { name: /Download PDF for October 2024/ }).click()
    await expect(page.getByText('Download started for the selected TTB report.')).toBeVisible()

    await page.getByRole('button', { name: /View report for October 2024/ }).click()
    await expect(page.getByRole('dialog', { name: /Form 5110.28/ })).toBeVisible()
    await page.getByRole('button', { name: 'Close report preview' }).click()
  })

  test('applies filters when fetching reports', async ({ page }) => {
    const requests: string[] = []

    await page.route('**/api/ttb/reports?**', route => {
      requests.push(route.request().url())
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sampleReports) })
    })

    await page.goto('/ttb-reports')

    await page.getByLabel('Status').selectOption('Approved')
    await page.getByLabel('Year').selectOption('2023')

    await page.waitForTimeout(100)

    const lastRequest = requests.at(-1) ?? ''
    expect(lastRequest).toContain('status=Approved')
    expect(lastRequest).toContain('year=2023')
  })
})
