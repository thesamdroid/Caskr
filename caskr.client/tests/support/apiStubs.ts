import type { Page } from '@playwright/test'

export interface DashboardStubOptions {
  orders?: unknown[]
  statuses?: unknown[]
  tasksByOrder?: Record<number, unknown[]>
  users?: unknown[]
}

export const stubDashboardData = async (page: Page, options: DashboardStubOptions = {}) => {
  const orders = options.orders ?? [
    { id: 1, name: 'Sample Order', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 5, mashBillId: 1 }
  ]

  const statuses = options.statuses ?? [
    { id: 1, name: 'In Progress', statusTasks: [] }
  ]

  const tasksByOrder = options.tasksByOrder ?? {}

  const users = options.users ?? [
    { id: 1, name: 'Alex Brewer', companyId: 1 }
  ]

  await page.route('**/api/status', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(statuses)
    })
  })

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

  await page.route(/.*\/api\/orders\/\d+\/tasks$/, async route => {
    const url = new URL(route.request().url())
    const match = /\/api\/orders\/(\d+)\/tasks$/.exec(url.pathname)
    const orderId = match ? Number(match[1]) : undefined
    const tasks = tasksByOrder[orderId ?? 0] ?? []
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(tasks)
    })
  })

  await page.route('**/api/users', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(users)
    })
  })
}

export const stubBarrelsData = async (page: Page, barrels: unknown[] = []) => {
  await page.route('**/api/barrels/company/1', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(barrels)
    })
  })
}
