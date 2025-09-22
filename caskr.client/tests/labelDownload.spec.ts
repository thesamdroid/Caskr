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

  await page.route('**/api/orders/1/outstanding-tasks', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
  })
}

test.describe('label document generation', () => {
  test('downloads the generated TTB PDF', async ({ page }) => {
    await setupOrdersPageRoutes(page)

    const pdfContent = '%PDF-1.4\n%Mock PDF content\n'
    await page.route('**/api/labels/ttb-form', async route => {
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
    await page.getByRole('button', { name: 'Generate TTB Document' }).click()

    await page.fill('input[placeholder="Brand Name"]', 'Mock Brand')
    await page.fill('input[placeholder="Product Name"]', 'Mock Product')
    await page.fill('input[placeholder="Alcohol Content"]', '40%')

    const downloadPromise = page.waitForEvent('download')
    await page.getByRole('button', { name: 'Generate' }).click()
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
    await page.getByRole('button', { name: 'Generate TTB Document' }).click()

    await page.fill('input[placeholder="Brand Name"]', 'Mock Brand')
    await page.fill('input[placeholder="Product Name"]', 'Mock Product')
    await page.fill('input[placeholder="Alcohol Content"]', '40%')

    const downloadPromise = page.waitForEvent('download')
    await page.getByRole('button', { name: 'Generate' }).click()
    await downloadPromise

    expect(capturedContentType).toBe('application/json')
    expect(capturedAccept).toBe('application/pdf')
    expect(capturedBody).not.toBeNull()

    const payload = JSON.parse(capturedBody!)
    expect(payload).toMatchObject({
      brandName: 'Mock Brand',
      productName: 'Mock Product',
      alcoholContent: '40%'
    })
    expect(typeof payload.companyId).toBe('number')
  })
})
