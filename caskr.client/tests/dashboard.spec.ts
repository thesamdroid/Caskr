import { test, expect } from '@playwright/test'

test.describe('dashboard active orders', () => {
  test('opens order actions modal when clicking an active order', async ({ page }) => {
    await page.route('**/api/status', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 1, name: 'In Progress', statusTasks: [{ id: 10, name: 'Review paperwork' }] }
        ])
      })
    })

    await page.route('**/api/orders', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: 1, name: 'Bourbon Order', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 10, mashBillId: 1 }
          ])
        })
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' })
      }
    })

    await page.route('**/api/orders/1/tasks', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 10, name: 'Review paperwork', orderId: 1, assigneeId: null, isComplete: false }
        ])
      })
    })

    await page.route('**/api/barrels/company/1', async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
    })

    await page.goto('/')

    await expect(page.getByRole('heading', { level: 3, name: 'Bourbon Order' })).toBeVisible()

    await page.getByRole('button', { name: 'Expand' }).click()

    await expect(page.getByRole('heading', { level: 4, name: 'Tasks' })).toBeVisible()
    await expect(page.getByText('Review paperwork')).toBeVisible()
  })
})
