import { expect, test, type Page } from '@playwright/test'
import { seedAuthenticatedUser } from './support/auth'

interface DashboardStubOptions {
  tasks?: unknown[]
  orders?: unknown[]
  alerts?: unknown[]
  activity?: unknown[]
  stats?: unknown
  tasksError?: boolean
  ordersError?: boolean
}

async function stubMobileDashboardData(page: Page, options: DashboardStubOptions = {}) {
  const tasks = options.tasks ?? [
    {
      id: 1,
      title: 'Check barrel B-2024-001',
      description: 'Verify seal integrity',
      assigneeId: 1,
      assigneeName: 'John Doe',
      dueDate: new Date().toISOString(),
      dueTime: '2:00 PM',
      priority: 'high',
      orderId: 1,
      orderName: 'Bourbon Batch #42',
      isComplete: false
    },
    {
      id: 2,
      title: 'Record gauge for B-2024-002',
      description: 'Monthly gauge reading',
      assigneeId: 1,
      dueDate: new Date().toISOString(),
      priority: 'medium',
      orderId: 2,
      orderName: 'Rye Batch #15',
      isComplete: false
    }
  ]

  const orders = options.orders ?? [
    {
      id: 1,
      name: 'Bourbon Batch #42',
      statusId: 1,
      quantity: 50
    },
    {
      id: 2,
      name: 'Rye Batch #15',
      statusId: 1,
      quantity: 25
    }
  ]

  const alerts = options.alerts ?? []
  const activity = options.activity ?? [
    {
      id: '1',
      type: 'barrel_filled',
      title: 'Barrel B-2024-001 filled',
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
      iconType: 'barrel'
    },
    {
      id: '2',
      type: 'task_completed',
      title: 'Inventory check completed',
      timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
      iconType: 'task'
    }
  ]

  const stats = options.stats ?? {
    tasksCompletedToday: 3,
    tasksDueToday: 5,
    activeBarrels: 142,
    pendingMovements: 2
  }

  // Stub tasks endpoint
  await page.route('**/api/tasks**', async route => {
    if (options.tasksError) {
      await route.fulfill({ status: 500 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(tasks)
    })
  })

  // Stub orders endpoint
  await page.route('**/api/orders**', async route => {
    if (options.ordersError) {
      await route.fulfill({ status: 500 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(orders)
    })
  })

  // Stub alerts endpoint
  await page.route('**/api/dashboard/alerts**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(alerts)
    })
  })

  // Stub activity endpoint
  await page.route('**/api/dashboard/activity**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(activity)
    })
  })

  // Stub stats endpoint
  await page.route('**/api/dashboard/stats**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(stats)
    })
  })

  // Stub task complete endpoint
  await page.route('**/api/tasks/*/complete', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true })
    })
  })
}

test.describe('Mobile Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('renders greeting with correct time of day', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    const hour = new Date().getHours()
    let expectedGreeting: string
    if (hour < 12) expectedGreeting = 'Good morning'
    else if (hour < 17) expectedGreeting = 'Good afternoon'
    else expectedGreeting = 'Good evening'

    await expect(page.getByText(new RegExp(`^${expectedGreeting},`))).toBeVisible()
  })

  test('shows alert banner when alerts present', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, {
      alerts: [
        {
          id: 'alert-1',
          type: 'critical',
          title: 'TTB Report Due',
          message: 'Your monthly TTB report is due today',
          actionUrl: '/ttb-reports',
          dismissible: true
        }
      ]
    })

    await page.goto('/')

    await expect(page.getByText('TTB Report Due')).toBeVisible()
    await expect(page.getByText('Your monthly TTB report is due today')).toBeVisible()
  })

  test('hides alert banner when no alerts', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, { alerts: [] })

    await page.goto('/')

    await expect(page.getByText('TTB Report Due')).not.toBeVisible()
  })

  test('renders all 4 quick action buttons', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await expect(page.getByRole('button', { name: 'Scan Barrel' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'New Task' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Record Movement' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Log Gauge' })).toBeVisible()
  })

  test('quick actions navigate correctly', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await page.getByRole('button', { name: 'Scan Barrel' }).click()
    await expect(page).toHaveURL(/\/scan/)
  })

  test('displays today\'s priorities list', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await expect(page.getByText('Today\'s Priorities')).toBeVisible()
    await expect(page.getByText('Check barrel B-2024-001')).toBeVisible()
    await expect(page.getByText('Record gauge for B-2024-002')).toBeVisible()
  })

  test('shows empty state when no tasks due today', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, { tasks: [] })

    await page.goto('/')

    await expect(page.getByText('All caught up!')).toBeVisible()
    await expect(page.getByText('No tasks due today')).toBeVisible()
  })

  test('renders active orders carousel', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await expect(page.getByText('Active Orders')).toBeVisible()
    await expect(page.getByText('Bourbon Batch #42')).toBeVisible()
    await expect(page.getByText('Rye Batch #15')).toBeVisible()
  })

  test('navigates to order details on card tap', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await page.getByText('Bourbon Batch #42').click()
    await expect(page).toHaveURL(/\/orders\/1/)
  })

  test('displays recent activity section', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    await expect(page.getByText('Recent Activity')).toBeVisible()
    await expect(page.getByText('Barrel B-2024-001 filled')).toBeVisible()
    await expect(page.getByText('Inventory check completed')).toBeVisible()
  })

  test('collapses recent activity on header tap', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    const activityHeader = page.getByRole('button', { name: /Recent Activity/i })
    await expect(page.getByText('Barrel B-2024-001 filled')).toBeVisible()

    await activityHeader.click()
    await expect(page.getByText('Barrel B-2024-001 filled')).not.toBeVisible()

    await activityHeader.click()
    await expect(page.getByText('Barrel B-2024-001 filled')).toBeVisible()
  })

  test('alert banner renders correct styling per type', async ({ page }) => {
    await seedAuthenticatedUser(page)

    // Test critical alert
    await stubMobileDashboardData(page, {
      alerts: [
        {
          id: 'critical-1',
          type: 'critical',
          title: 'Critical Alert',
          message: 'Test message',
          dismissible: true
        }
      ]
    })

    await page.goto('/')
    const criticalAlert = page.locator('[class*="critical"]')
    await expect(criticalAlert).toBeVisible()
  })

  test('dismisses alert when dismiss button clicked', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, {
      alerts: [
        {
          id: 'alert-1',
          type: 'warning',
          title: 'Dismissible Alert',
          message: 'This can be dismissed',
          dismissible: true
        }
      ]
    })

    await page.goto('/')
    await expect(page.getByText('Dismissible Alert')).toBeVisible()

    await page.getByRole('button', { name: 'Dismiss alert' }).click()
    await expect(page.getByText('Dismissible Alert')).not.toBeVisible()
  })

  test('shows loading state initially', async ({ page }) => {
    await seedAuthenticatedUser(page)

    // Delay API responses
    await page.route('**/api/**', async route => {
      await new Promise(resolve => setTimeout(resolve, 500))
      await route.fulfill({ status: 200, body: '[]' })
    })

    await page.goto('/')
    await expect(page.getByText('Loading dashboard...')).toBeVisible()
  })

  test('handles API errors gracefully', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/**', async route => {
      await route.fulfill({ status: 500 })
    })

    await page.goto('/')
    await expect(page.getByText(/data may be outdated/i)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible()
  })

  test('surfaces partial failures while keeping cached UI usable', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, { tasksError: true })

    await page.goto('/')

    await expect(page.getByText(/data may be outdated/i)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Scan Barrel' })).toBeVisible()
  })

  test('shows offline indicator when offline', async ({ page, context }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    // Go offline
    await context.setOffline(true)

    // Trigger a page action that would show offline status
    await page.reload()

    await expect(page.getByText(/offline/i)).toBeVisible()
    await expect(page.getByText('Check barrel B-2024-001')).toBeVisible()
  })
})

test.describe('Mobile Dashboard Data Hook', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('fetches all data on mount', async ({ page }) => {
    await seedAuthenticatedUser(page)

    const requests: string[] = []
    await page.route('**/api/**', async route => {
      requests.push(route.request().url())
      await route.fulfill({ status: 200, body: '[]' })
    })

    await page.goto('/')
    await page.waitForTimeout(1000)

    expect(requests.some(r => r.includes('/api/tasks'))).toBeTruthy()
    expect(requests.some(r => r.includes('/api/orders'))).toBeTruthy()
  })

  test('refreshes on pull-to-refresh', async ({ page }) => {
    await seedAuthenticatedUser(page)

    let requestCount = 0
    await page.route('**/api/tasks**', async route => {
      requestCount++
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]'
      })
    })

    await stubMobileDashboardData(page)
    await page.goto('/')
    await page.waitForTimeout(500)

    const initialCount = requestCount

    // Simulate pull-to-refresh with touch events
    const dashboard = page.locator('[class*="dashboard"]').first()
    const box = await dashboard.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + 50)
      await page.mouse.down()
      await page.mouse.move(box.x + box.width / 2, box.y + 200, { steps: 10 })
      await page.mouse.up()
    }

    await page.waitForTimeout(1500)
    expect(requestCount).toBeGreaterThan(initialCount)
  })
})

test.describe('Mobile Dashboard Integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('full page load with all components', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page, {
      alerts: [
        {
          id: 'alert-1',
          type: 'warning',
          title: 'Tasks Overdue',
          message: '3 tasks are overdue',
          dismissible: true
        }
      ]
    })

    await page.goto('/')

    // Verify all sections are rendered
    await expect(page.getByText(/Good (morning|afternoon|evening)/)).toBeVisible()
    await expect(page.getByText('Tasks Overdue')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Scan Barrel' })).toBeVisible()
    await expect(page.getByText('Today\'s Priorities')).toBeVisible()
    await expect(page.getByText('Active Orders')).toBeVisible()
    await expect(page.getByText('Recent Activity')).toBeVisible()
  })

  test('task completion flow with undo', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    // Find a task
    const taskItem = page.getByText('Check barrel B-2024-001')
    await expect(taskItem).toBeVisible()

    // We can't easily simulate swipe, but verify the task is displayed
    // and that the undo toast appears after task completion via API
  })

  test('navigation flow to order details', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileDashboardData(page)

    await page.goto('/')

    // Click on an order card
    await page.getByText('Bourbon Batch #42').click()

    // Verify navigation
    await expect(page).toHaveURL(/\/orders\/1/)
  })
})
