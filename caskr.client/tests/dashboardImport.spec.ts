import { test, expect } from '@playwright/test';

type Task = {
  id: number;
  name: string;
  orderId: number;
  assigneeId: number | null;
  isComplete: boolean;
};

test('dashboard displays stats overview and current orders', async ({ page }) => {
  await page.route('**/api/status', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, name: 'In Progress', statusTasks: [] },
        { id: 2, name: 'Completed', statusTasks: [] }
      ])
    })
  })

  await page.route('**/api/users', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
  })

  const orders = [
    { id: 1, name: 'Bourbon Order', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 10, mashBillId: 1 },
    { id: 2, name: 'Rye Order', statusId: 2, ownerId: 1, spiritTypeId: 1, quantity: 5, mashBillId: 2 }
  ]

  const tasksByOrder: Record<number, Task[]> = {
    1: [
      { id: 10, name: 'Review paperwork', orderId: 1, assigneeId: null, isComplete: false },
      { id: 11, name: 'Schedule bottling', orderId: 1, assigneeId: 2, isComplete: true }
    ],
    2: [{ id: 20, name: 'Finalize labels', orderId: 2, assigneeId: 3, isComplete: true }]
  }

  await page.route('**/api/orders', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(orders)
      })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' })
    }
  })

  await page.route('**/api/orders/*/tasks', async route => {
    const url = new URL(route.request().url())
    const orderId = Number(url.pathname.split('/')[3])
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(tasksByOrder[orderId] ?? [])
    })
  })

  await page.goto('/')

  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard' })).toBeVisible()
  await expect(page.getByRole('heading', { level: 2, name: 'Current Orders' })).toBeVisible()

  const statCards = page.locator('.stat-card')
  await expect(statCards).toHaveCount(4)
  await expect(statCards.filter({ hasText: 'Active Orders' })).toContainText('2')
  await expect(statCards.filter({ hasText: 'Completed Orders' })).toContainText('1')
  await expect(statCards.filter({ hasText: 'Total Tasks' })).toContainText('3')
  await expect(statCards.filter({ hasText: 'Overall Progress' })).toContainText('67%')

  await expect(page.getByRole('heading', { level: 3, name: 'Bourbon Order' })).toBeVisible()
  await expect(page.getByRole('heading', { level: 3, name: 'Rye Order' })).toBeVisible()
  await expect(page.getByRole('button', { name: /Import/i })).toHaveCount(0)
})
