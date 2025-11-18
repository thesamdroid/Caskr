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

interface QuickBooksStubOptions {
  status?: Record<string, unknown>
  accounts?: unknown[]
  mappings?: unknown[]
  preferences?: Record<string, unknown>
}

export const stubQuickBooksAccountingData = async (
  page: Page,
  options: QuickBooksStubOptions = {}
) => {
  const status =
    options.status ?? {
      connected: true,
      realmId: '1234567890',
      connectedAt: new Date('2024-02-15T12:00:00Z').toISOString()
    }

  const accounts =
    options.accounts ?? [
      { id: 'acct-1', name: 'COGS - Finished Goods', accountType: 'Expense', active: true },
      { id: 'acct-2', name: 'Inventory Asset', accountType: 'Other Current Asset', active: true }
    ]

  const mappings =
    options.mappings ?? [
      { caskrAccountType: 'Cogs', qboAccountId: 'acct-1', qboAccountName: 'COGS - Finished Goods' },
      { caskrAccountType: 'WorkInProgress', qboAccountId: 'acct-2', qboAccountName: 'Inventory Asset' }
    ]

  let preferences =
    options.preferences ?? {
      companyId: 1,
      autoSyncInvoices: true,
      autoSyncCogs: false,
      syncFrequency: 'Hourly'
    }

  await page.route('**/api/accounting/quickbooks/status**', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(status) })
  })

  await page.route('**/api/accounting/quickbooks/accounts**', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(accounts) })
  })

  await page.route('**/api/accounting/quickbooks/mappings**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mappings) })
      return
    }

    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mappings) })
  })

  await page.route('**/api/accounting/quickbooks/preferences**', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(preferences) })
      return
    }

    const body = route.request().postDataJSON?.() ?? {}
    preferences = { ...preferences, ...body }
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(preferences) })
  })

  await page.route('**/api/accounting/quickbooks/test**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'QuickBooks connection verified.' })
    })
  })
}

interface SyncLogStubOptions {
  logs?: Array<Record<string, unknown>>
  total?: number
}

export const stubQuickBooksSyncLogs = async (page: Page, options: SyncLogStubOptions = {}) => {
  const logs =
    options.logs ??
    [
      {
        id: '1',
        syncedAt: new Date('2024-05-05T12:00:00Z').toISOString(),
        entityType: 'Invoice',
        entityId: 101,
        status: 'Failed',
        qboId: null,
        errorMessage: 'Customer missing in QuickBooks.'
      },
      {
        id: '2',
        syncedAt: new Date('2024-05-06T15:30:00Z').toISOString(),
        entityType: 'Batch',
        entityId: 45,
        status: 'Success',
        qboId: 'QB-67890',
        errorMessage: null
      }
    ]

  const total = options.total ?? logs.length

  await page.route('**/api/accounting/quickbooks/sync-logs**', async route => {
    const url = new URL(route.request().url())
    const requestedPage = Number(url.searchParams.get('page') ?? '1')
    const limit = Number(url.searchParams.get('limit') ?? '25')
    const start = (requestedPage - 1) * limit
    const pageData = logs.slice(start, start + limit)
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: pageData, total, page: requestedPage, limit })
    })
  })

  await page.route('**/api/accounting/quickbooks/sync-invoice', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true })
    })
  })

  await page.route('**/api/accounting/quickbooks/sync-batch', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true })
    })
  })
}
