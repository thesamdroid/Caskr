import { expect, test, type Page } from '@playwright/test'
import { seedAuthenticatedUser } from './support/auth'

interface TasksStubOptions {
  tasks?: unknown[]
  error?: boolean
}

async function stubMobileTasksData(page: Page, options: TasksStubOptions = {}) {
  const today = new Date().toISOString().split('T')[0]
  const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().split('T')[0]

  const tasks = options.tasks ?? [
    {
      id: 1,
      title: 'Check barrel B-2024-001',
      description: 'Verify seal integrity',
      assigneeId: 1,
      assigneeName: 'John Doe',
      dueDate: today,
      dueTime: '2:00 PM',
      priority: 'high',
      orderId: 1,
      orderName: 'Bourbon Batch #42',
      isComplete: false,
      createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString()
    },
    {
      id: 2,
      title: 'Record gauge for B-2024-002',
      description: 'Monthly gauge reading',
      assigneeId: 1,
      assigneeName: 'John Doe',
      dueDate: today,
      priority: 'medium',
      orderId: 2,
      orderName: 'Rye Batch #15',
      isComplete: false,
      createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString()
    },
    {
      id: 3,
      title: 'Move barrels to Warehouse B',
      description: '12 barrels total',
      assigneeId: 1,
      dueDate: tomorrow,
      priority: 'low',
      isComplete: false,
      createdAt: new Date().toISOString()
    },
    {
      id: 4,
      title: 'Overdue task',
      description: 'This should have been done',
      assigneeId: 1,
      dueDate: yesterday,
      priority: 'high',
      isComplete: false,
      createdAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString()
    },
    {
      id: 5,
      title: 'Completed task',
      description: 'Already done',
      assigneeId: 1,
      dueDate: today,
      priority: 'medium',
      isComplete: true,
      createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString()
    }
  ]

  // Stub tasks endpoint
  await page.route('**/api/tasks**', async route => {
    if (options.error) {
      await route.fulfill({ status: 500 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(tasks)
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

  // Stub task delete endpoint
  await page.route('**/api/tasks/*', async route => {
    if (route.request().method() === 'DELETE') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true })
      })
    } else {
      await route.continue()
    }
  })
}

test.describe('Mobile Tasks Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('renders task list with groups', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Check for group headers
    await expect(page.getByText('Overdue')).toBeVisible()
    await expect(page.getByText('Today')).toBeVisible()
    await expect(page.getByText('Tomorrow')).toBeVisible()
  })

  test('displays task progress bar', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    await expect(page.getByText("Today's Progress")).toBeVisible()
  })

  test('renders view mode tabs', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    await expect(page.getByRole('button', { name: 'My Tasks' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'All Tasks' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Completed' })).toBeVisible()
  })

  test('switches view mode on tab click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Click on Completed tab
    await page.getByRole('button', { name: 'Completed' }).click()

    // Should show completed tasks
    await expect(page.getByText('Completed task')).toBeVisible()
  })

  test('displays filter toggle button', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    await expect(page.getByText('Filters')).toBeVisible()
  })

  test('expands filters on toggle click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Click filters toggle
    await page.getByText('Filters').click()

    // Filter groups should be visible
    await expect(page.getByText('Priority')).toBeVisible()
    await expect(page.getByText('Due Date')).toBeVisible()
    await expect(page.getByText('Sort By')).toBeVisible()
  })

  test('filters by priority', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Expand filters
    await page.getByText('Filters').click()

    // Click high priority chip
    await page.getByRole('button', { name: 'High' }).click()

    // Should only show high priority tasks
    await expect(page.getByText('Check barrel B-2024-001')).toBeVisible()
    await expect(page.getByText('Overdue task')).toBeVisible()
    // Medium priority should be hidden
    await expect(page.getByText('Record gauge for B-2024-002')).not.toBeVisible()
  })

  test('sorts tasks by due date', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Tasks should be sorted by due date by default
    // Overdue should appear first
    const firstTaskGroup = page.locator('[class*="group"]').first()
    await expect(firstTaskGroup.getByText('Overdue')).toBeVisible()
  })

  test('resets filters on reset button click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Expand and apply a filter
    await page.getByText('Filters').click()
    await page.getByRole('button', { name: 'High' }).click()

    // Reset filters
    await page.getByRole('button', { name: 'Reset Filters' }).click()

    // All tasks should be visible again
    await expect(page.getByText('Record gauge for B-2024-002')).toBeVisible()
  })

  test('shows FAB for creating new task', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    await expect(page.getByRole('button', { name: 'Create new task' })).toBeVisible()
  })

  test('opens task form sheet on FAB click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    await page.getByRole('button', { name: 'Create new task' }).click()

    // Form sheet should be visible
    await expect(page.getByText('New Task')).toBeVisible()
    await expect(page.getByPlaceholder('Enter task title')).toBeVisible()
  })

  test('displays task detail sheet on task click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Click on a task
    await page.getByText('Check barrel B-2024-001').click()

    // Detail sheet should show
    await expect(page.getByText('High Priority')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Mark Complete' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Edit' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Delete' })).toBeVisible()
  })

  test('closes detail sheet on drag down', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open task detail
    await page.getByText('Check barrel B-2024-001').click()
    await expect(page.getByRole('button', { name: 'Mark Complete' })).toBeVisible()

    // Click outside to close (on backdrop)
    await page.locator('[class*="overlay"]').click({ position: { x: 50, y: 50 } })

    // Sheet should be hidden
    await expect(page.getByRole('button', { name: 'Mark Complete' })).not.toBeVisible()
  })

  test('shows empty state when no tasks', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page, { tasks: [] })

    await page.goto('/tasks')

    await expect(page.getByText('No tasks yet')).toBeVisible()
  })

  test('shows error state on API failure', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page, { error: true })

    await page.goto('/tasks')

    await expect(page.getByText('Try Again')).toBeVisible()
  })

  test('shows loading skeleton initially', async ({ page }) => {
    await seedAuthenticatedUser(page)

    await page.route('**/api/tasks**', async route => {
      await new Promise(resolve => setTimeout(resolve, 1000))
      await route.fulfill({ status: 200, body: '[]' })
    })

    await page.goto('/tasks')

    // Should show skeleton loading
    await expect(page.locator('[class*="skeleton"]').first()).toBeVisible()
  })
})

test.describe('Mobile Tasks Multi-Select', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('enters multi-select mode on long press', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Simulate long press by holding down
    const taskItem = page.getByText('Check barrel B-2024-001')
    const box = await taskItem.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2)
      await page.mouse.down()
      await page.waitForTimeout(600)
      await page.mouse.up()
    }

    // Multi-select toolbar should appear
    await expect(page.getByText('1 selected')).toBeVisible()
  })

  test('shows multi-select toolbar with actions', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Simulate long press
    const taskItem = page.getByText('Check barrel B-2024-001')
    const box = await taskItem.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2)
      await page.mouse.down()
      await page.waitForTimeout(600)
      await page.mouse.up()
    }

    // Toolbar buttons should be visible
    await expect(page.getByRole('button', { name: 'Cancel' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Select All' })).toBeVisible()
  })

  test('exits multi-select mode on cancel', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Enter multi-select mode
    const taskItem = page.getByText('Check barrel B-2024-001')
    const box = await taskItem.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2)
      await page.mouse.down()
      await page.waitForTimeout(600)
      await page.mouse.up()
    }

    await expect(page.getByText('1 selected')).toBeVisible()

    // Cancel
    await page.getByRole('button', { name: 'Cancel' }).click()

    // Multi-select toolbar should be hidden
    await expect(page.getByText('1 selected')).not.toBeVisible()
  })

  test('selects all tasks on select all click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Enter multi-select mode
    const taskItem = page.getByText('Check barrel B-2024-001')
    const box = await taskItem.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2)
      await page.mouse.down()
      await page.waitForTimeout(600)
      await page.mouse.up()
    }

    // Click select all
    await page.getByRole('button', { name: 'Select All' }).click()

    // Should show all visible tasks selected (4 incomplete tasks)
    await expect(page.getByText('4 selected')).toBeVisible()
  })
})

test.describe('Mobile Tasks Form', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('validates required fields', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open new task form
    await page.getByRole('button', { name: 'Create new task' }).click()

    // Try to save without title
    await page.getByRole('button', { name: 'Save' }).click()

    // Should show error
    await expect(page.getByText('Title is required')).toBeVisible()
  })

  test('saves new task with valid data', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    let savedTaskData: unknown = null
    await page.route('**/api/tasks', async route => {
      if (route.request().method() === 'POST') {
        savedTaskData = JSON.parse(route.request().postData() || '{}')
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: 100, ...savedTaskData })
        })
      } else {
        await route.continue()
      }
    })

    await page.goto('/tasks')

    // Open new task form
    await page.getByRole('button', { name: 'Create new task' }).click()

    // Fill out form
    await page.getByPlaceholder('Enter task title').fill('New test task')
    await page.getByRole('button', { name: 'Save' }).click()

    // Form should close
    await expect(page.getByText('New Task')).not.toBeVisible()
  })

  test('cancels form without saving', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open new task form
    await page.getByRole('button', { name: 'Create new task' }).click()

    // Fill out form
    await page.getByPlaceholder('Enter task title').fill('Unsaved task')

    // Cancel
    await page.getByRole('button', { name: 'Cancel' }).click()

    // Form should close
    await expect(page.getByText('New Task')).not.toBeVisible()
  })

  test('priority buttons are selectable', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open new task form
    await page.getByRole('button', { name: 'Create new task' }).click()

    // Select high priority
    await page.getByRole('button', { name: 'High' }).click()

    // Button should be selected (have active class)
    await expect(page.getByRole('button', { name: 'High' })).toHaveClass(/selected/)
  })
})

test.describe('Mobile Tasks Detail Sheet', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('displays all task details', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open task detail
    await page.getByText('Check barrel B-2024-001').click()

    // Verify details
    await expect(page.getByText('High Priority')).toBeVisible()
    await expect(page.getByText('Check barrel B-2024-001')).toBeVisible()
    await expect(page.getByText('Verify seal integrity')).toBeVisible()
    await expect(page.getByText('Due Date')).toBeVisible()
    await expect(page.getByText('Assigned to')).toBeVisible()
    await expect(page.getByText('John Doe')).toBeVisible()
    await expect(page.getByText('Order')).toBeVisible()
    await expect(page.getByText('Bourbon Batch #42')).toBeVisible()
  })

  test('marks task complete from detail sheet', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open task detail
    await page.getByText('Check barrel B-2024-001').click()

    // Click complete button
    await page.getByRole('button', { name: 'Mark Complete' }).click()

    // Sheet should close
    await expect(page.getByRole('button', { name: 'Mark Complete' })).not.toBeVisible()
  })

  test('shows delete confirmation', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open task detail
    await page.getByText('Check barrel B-2024-001').click()

    // Click delete button once
    await page.getByRole('button', { name: 'Delete' }).click()

    // Should show confirm state
    await expect(page.getByRole('button', { name: 'Confirm' })).toBeVisible()
  })

  test('opens edit form from detail sheet', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileTasksData(page)

    await page.goto('/tasks')

    // Open task detail
    await page.getByText('Check barrel B-2024-001').click()

    // Click edit button
    await page.getByRole('button', { name: 'Edit' }).click()

    // Edit form should open with task data
    await expect(page.getByText('Edit Task')).toBeVisible()
    await expect(page.getByDisplayValue('Check barrel B-2024-001')).toBeVisible()
  })
})

test.describe('Mobile Tasks Pull-to-Refresh', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('refreshes task list on pull down', async ({ page }) => {
    await seedAuthenticatedUser(page)

    let requestCount = 0
    await page.route('**/api/tasks**', async route => {
      requestCount++
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      })
    })

    await page.goto('/tasks')
    await page.waitForTimeout(500)

    const initialCount = requestCount

    // Simulate pull-to-refresh
    const list = page.locator('[class*="container"]').first()
    const box = await list.boundingBox()
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
