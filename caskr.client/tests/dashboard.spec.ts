import { test, expect } from '@playwright/test'

test.describe('dashboard active orders', () => {
  test('shows outstanding tasks when expanding an order card', async ({ page }) => {
    await page.route('**/api/status', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 1, name: 'In Progress', statusTasks: [{ id: 10, name: 'Review paperwork' }] }
        ])
      })
    })

    await page.route('**/api/users', async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
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
        body: JSON.stringify([{ id: 10, name: 'Review paperwork', orderId: 1, assigneeId: null, isComplete: false }])
      })
    })

    await page.goto('/')

    const orderCard = page.locator('.order-card').filter({
      has: page.getByRole('heading', { level: 3, name: 'Bourbon Order' })
    })

    await expect(orderCard).toBeVisible()
    await orderCard.getByRole('button', { name: 'Expand' }).click()
    await expect(orderCard.getByRole('heading', { level: 4, name: 'Tasks' })).toBeVisible()
    await expect(orderCard.getByText('Review paperwork')).toBeVisible()
  })
})
