import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

const mockOrders = [
  { id: 1, name: 'TTB Filing Order', statusId: 2, ownerId: 1, spiritTypeId: 1, quantity: 10, mashBillId: 1 }
]

const mockStatuses = [
  { id: 2, name: 'Awaiting TTB Approval', statusTasks: [] }
]

const setupOrdersPageRoutes = async (page: Page) => {
  await page.route('**/api/status', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockStatuses)
    })
  })

  await page.route('**/api/orders', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockOrders)
      })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' })
    }
  })

  await page.route('**/api/orders/1/tasks', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
  })
}

test.describe('label document generation', () => {
  test('downloads the generated TTB PDF after previewing it', async ({ page }) => {
    await setupOrdersPageRoutes(page)

    const pdfContent = '%PDF-1.4\n%Mock PDF content\n'
    let requestCount = 0
    await page.route('**/api/labels/ttb-form', async route => {
      requestCount += 1
      expect(route.request().method()).toBe('POST')
      await route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'application/pdf'
        },
        body: pdfContent
      })
    })

    await page.goto('/orders')

    await page.getByRole('row', { name: /TTB Filing Order/ }).click()
    await page.getByRole('button', { name: 'Generate TTB Label' }).click()

    await page.getByLabel('Brand Name').fill('Mock Brand')
    await page.getByLabel('Product Name').fill('Mock Product')
    await page.getByLabel('Alcohol Content').fill('40%')

    await page.getByRole('button', { name: 'Generate Label' }).click()

    await expect.poll(() => requestCount).toBe(1)

    const previewRegion = page.getByRole('region', { name: 'Generated TTB document preview' })
    await expect(previewRegion).toBeVisible()

    const downloadPromise = page.waitForEvent('download')
    await page.getByRole('button', { name: 'Download PDF' }).click()
    const download = await downloadPromise

    expect(download.suggestedFilename()).toBe('ttb_form_5100_31.pdf')

    const stream = await download.createReadStream()
    if (!stream) {
      throw new Error('Expected download stream to be available')
    }
    const chunks: Buffer[] = []
    for await (const chunk of stream) {
      chunks.push(Buffer.from(chunk))
    }
    const downloadedContent = Buffer.concat(chunks).toString()
    expect(downloadedContent).toBe(pdfContent)
  })

  test('sends label generation requests as JSON', async ({ page }) => {
    await setupOrdersPageRoutes(page)

    const pdfContent = '%PDF-1.4\n%Mock PDF content\n'
    let capturedContentType: string | null = null
    let capturedAccept: string | null = null
    let capturedBody: string | null = null

    await page.route('**/api/labels/ttb-form', async route => {
      const request = route.request()
      capturedContentType = request.headerValue('content-type')
      capturedAccept = request.headerValue('accept')
      capturedBody = request.postData() ?? null
      await route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'application/pdf'
        },
        body: pdfContent
      })
    })

    await page.goto('/orders')

    await page.getByRole('row', { name: /TTB Filing Order/ }).click()
    await page.getByRole('button', { name: 'Generate TTB Label' }).click()

    await page.getByLabel('Brand Name').fill('Mock Brand')
    await page.getByLabel('Product Name').fill('Mock Product')
    await page.getByLabel('Alcohol Content').fill('40%')

    await page.getByRole('button', { name: 'Generate Label' }).click()

    await expect.poll(() => capturedContentType).toBe('application/json')
    await expect.poll(() => capturedAccept).toBe('application/pdf')
    await expect.poll(() => capturedBody).not.toBeNull()

    const previewRegion = page.getByRole('region', { name: 'Generated TTB document preview' })
    await expect(previewRegion).toBeVisible()

    const payload = JSON.parse(capturedBody!)
    expect(payload).toMatchObject({
      brandName: 'Mock Brand',
      productName: 'Mock Product',
      alcoholContent: '40%'
    })
    expect(typeof payload.companyId).toBe('number')
  })

  test('allows returning to the label form after previewing the document', async ({ page }) => {
    await setupOrdersPageRoutes(page)

    let requestCount = 0
    await page.route('**/api/labels/ttb-form', async route => {
      requestCount += 1
      await route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'application/pdf'
        },
        body: '%PDF-1.4\n%Mock PDF content\n'
      })
    })

    await page.goto('/orders')

    await page.getByRole('row', { name: /TTB Filing Order/ }).click()
    await page.getByRole('button', { name: 'Generate TTB Label' }).click()

    await page.getByLabel('Brand Name').fill('Mock Brand')
    await page.getByLabel('Product Name').fill('Mock Product')
    await page.getByLabel('Alcohol Content').fill('40%')

    const previewRegion = page.getByRole('region', { name: 'Generated TTB document preview' })
    const generateButton = page.getByRole('button', { name: 'Generate Label' })
    await generateButton.click()
    await expect.poll(() => requestCount).toBe(1)
    await expect(previewRegion).toBeVisible()

    await page.getByRole('button', { name: 'Back to form' }).click()
    await expect(previewRegion).toHaveCount(0)
    await expect(page.getByLabel('Brand Name')).toHaveValue('Mock Brand')
    await expect(page.getByLabel('Product Name')).toHaveValue('Mock Product')
    await expect(page.getByLabel('Alcohol Content')).toHaveValue('40%')

    await generateButton.click()
    await expect.poll(() => requestCount).toBe(2)
    await expect(previewRegion).toBeVisible()
  })
})
