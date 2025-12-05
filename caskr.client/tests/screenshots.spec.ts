import { test, expect, devices } from '@playwright/test'
import { stubDashboardData } from './support/apiStubs'
import { seedAuthenticatedUser } from './support/auth'

/**
 * Screenshot test suite for PR visual previews.
 * Captures dashboard, random pages, and error states in both desktop and mobile layouts.
 */

// Available pages to randomly select from (excluding dashboard which is always captured)
const availablePages = [
  { path: '/orders', name: 'orders', title: 'Orders' },
  { path: '/barrels', name: 'barrels', title: 'Barrels' },
  { path: '/warehouses', name: 'warehouses', title: 'Warehouses' },
  { path: '/products', name: 'products', title: 'Products' },
  { path: '/reports', name: 'reports', title: 'Reports' },
  { path: '/capacity', name: 'capacity', title: 'Capacity' },
  { path: '/ttb-reports', name: 'ttb-reports', title: 'TTB Reports' },
  { path: '/accounting', name: 'accounting', title: 'Accounting' },
  { path: '/purchase-orders', name: 'purchase-orders', title: 'Purchase Orders' },
]

// Use seeded random for reproducibility based on date (changes daily for variety)
function seededRandom(seed: number): () => number {
  return function() {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff
    return seed / 0x7fffffff
  }
}

function selectRandomPages(count: number): typeof availablePages {
  const today = new Date()
  const seed = today.getFullYear() * 10000 + (today.getMonth() + 1) * 100 + today.getDate()
  const random = seededRandom(seed)

  const shuffled = [...availablePages].sort(() => random() - 0.5)
  return shuffled.slice(0, count)
}

// Mobile viewport configuration
const mobileViewport = devices['iPhone 13']

// Common stub data for pages
const commonStubData = {
  orders: [
    { id: 1, name: 'Bourbon Batch #42', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 50, mashBillId: 1 },
    { id: 2, name: 'Rye Whiskey Reserve', statusId: 2, ownerId: 1, spiritTypeId: 2, quantity: 25, mashBillId: 2 },
    { id: 3, name: 'Single Malt Expression', statusId: 1, ownerId: 1, spiritTypeId: 3, quantity: 100, mashBillId: 3 },
  ],
  statuses: [
    { id: 1, name: 'In Progress', statusTasks: [{ id: 10, name: 'Quality Check' }, { id: 11, name: 'Barrel Fill' }] },
    { id: 2, name: 'Aging', statusTasks: [{ id: 20, name: 'Temperature Monitor' }] },
    { id: 3, name: 'Completed', statusTasks: [] },
  ],
  tasksByOrder: {
    1: [
      { id: 10, name: 'Quality Check', orderId: 1, assigneeId: 1, isComplete: false },
      { id: 11, name: 'Barrel Fill', orderId: 1, assigneeId: null, isComplete: false },
    ],
    2: [
      { id: 20, name: 'Temperature Monitor', orderId: 2, assigneeId: 1, isComplete: true },
    ],
    3: [],
  },
  users: [
    { id: 1, name: 'Master Distiller', companyId: 1 },
    { id: 2, name: 'Production Manager', companyId: 1 },
  ],
}

// Stub for generic pages that need basic API responses
async function stubGenericPageData(page: import('@playwright/test').Page) {
  await stubDashboardData(page, commonStubData)

  // Stub additional common endpoints
  await page.route('**/api/barrels/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, barrelNumber: 'BBN-001', spiritType: 'Bourbon', capacity: 53, currentVolume: 48, warehouseId: 1 },
        { id: 2, barrelNumber: 'BBN-002', spiritType: 'Rye', capacity: 53, currentVolume: 52, warehouseId: 1 },
        { id: 3, barrelNumber: 'BBN-003', spiritType: 'Bourbon', capacity: 30, currentVolume: 28, warehouseId: 2 },
      ])
    })
  })

  await page.route('**/api/warehouses/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, name: 'Rickhouse A', capacity: 500, currentCount: 245, location: 'Main Campus' },
        { id: 2, name: 'Rickhouse B', capacity: 300, currentCount: 180, location: 'North Facility' },
      ])
    })
  })

  await page.route('**/api/products/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, name: 'Small Batch Bourbon', sku: 'SBB-750', price: 45.99, status: 'Active' },
        { id: 2, name: 'Single Barrel Reserve', sku: 'SBR-750', price: 89.99, status: 'Active' },
      ])
    })
  })

  await page.route('**/api/purchase-orders/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, vendorName: 'Oak Supply Co', status: 'Pending', totalAmount: 12500, createdAt: '2024-01-15' },
        { id: 2, vendorName: 'Grain Masters', status: 'Received', totalAmount: 8900, createdAt: '2024-01-10' },
      ])
    })
  })

  await page.route('**/api/reports/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([])
    })
  })

  await page.route('**/api/capacity/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        totalCapacity: 800,
        currentUsage: 425,
        projectedUsage: 520,
        warehouses: [
          { name: 'Rickhouse A', capacity: 500, usage: 245 },
          { name: 'Rickhouse B', capacity: 300, usage: 180 },
        ]
      })
    })
  })

  await page.route('**/api/ttb/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([])
    })
  })

  await page.route('**/api/accounting/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ connected: false })
    })
  })

  await page.route('**/api/spirit-types/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, name: 'Bourbon' },
        { id: 2, name: 'Rye' },
        { id: 3, name: 'Single Malt' },
      ])
    })
  })

  await page.route('**/api/mash-bills/**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, name: 'Classic Bourbon Mash' },
        { id: 2, name: 'High Rye Mash' },
        { id: 3, name: 'Malted Barley' },
      ])
    })
  })
}

test.describe('PR Screenshots', () => {
  test.describe('Dashboard', () => {
    test('desktop layout', async ({ page }) => {
      await seedAuthenticatedUser(page, {
        permissions: ['TTB_COMPLIANCE', 'TTB_EDIT']
      })
      await stubDashboardData(page, commonStubData)

      await page.goto('/')
      await expect(page.locator('.stat-card').first()).toBeVisible({ timeout: 10000 })

      // Wait for content to fully render
      await page.waitForTimeout(500)

      await page.screenshot({
        path: 'test-results/screenshots/dashboard-desktop.png',
        fullPage: true,
      })
    })

    test('mobile layout', async ({ page }) => {
      await page.setViewportSize(mobileViewport.viewport)

      await seedAuthenticatedUser(page, {
        permissions: ['TTB_COMPLIANCE', 'TTB_EDIT']
      })
      await stubDashboardData(page, commonStubData)

      await page.goto('/')
      await expect(page.locator('.stat-card').first()).toBeVisible({ timeout: 10000 })

      await page.waitForTimeout(500)

      await page.screenshot({
        path: 'test-results/screenshots/dashboard-mobile.png',
        fullPage: true,
      })
    })
  })

  test.describe('Random Pages', () => {
    const selectedPages = selectRandomPages(3)

    for (const pageInfo of selectedPages) {
      test(`${pageInfo.name} - desktop layout`, async ({ page }) => {
        await seedAuthenticatedUser(page, {
          permissions: ['TTB_COMPLIANCE', 'TTB_EDIT']
        })
        await stubGenericPageData(page)

        await page.goto(pageInfo.path)

        // Wait for page to load
        await page.waitForLoadState('networkidle')
        await page.waitForTimeout(500)

        await page.screenshot({
          path: `test-results/screenshots/${pageInfo.name}-desktop.png`,
          fullPage: true,
        })
      })

      test(`${pageInfo.name} - mobile layout`, async ({ page }) => {
        await page.setViewportSize(mobileViewport.viewport)

        await seedAuthenticatedUser(page, {
          permissions: ['TTB_COMPLIANCE', 'TTB_EDIT']
        })
        await stubGenericPageData(page)

        await page.goto(pageInfo.path)

        await page.waitForLoadState('networkidle')
        await page.waitForTimeout(500)

        await page.screenshot({
          path: `test-results/screenshots/${pageInfo.name}-mobile.png`,
          fullPage: true,
        })
      })
    }
  })

  test.describe('Error States', () => {
    test('login error', async ({ page }) => {
      await page.route('**/api/auth/login', route =>
        route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Invalid email or password' })
        })
      )

      await page.goto('/login')
      await page.getByLabel('Email Address').fill('test@example.com')
      await page.getByLabel('Password').fill('wrongpassword')
      await page.click('button[type="submit"]')

      await expect(page.getByText('Invalid email or password')).toBeVisible()
      await page.waitForTimeout(300)

      await page.screenshot({
        path: 'test-results/screenshots/error-login.png',
        fullPage: true,
      })
    })

    test('dashboard load error', async ({ page }) => {
      await seedAuthenticatedUser(page)

      await page.route('**/api/status', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 1, name: 'In Progress', statusTasks: [] }])
        })
      })

      await page.route('**/api/orders', async route => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Database connection failed' })
        })
      })

      await page.route('**/api/users', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 1, name: 'Test User', companyId: 1 }])
        })
      })

      await page.goto('/')

      const alert = page.getByRole('alert')
      await expect(alert).toBeVisible({ timeout: 10000 })
      await page.waitForTimeout(300)

      await page.screenshot({
        path: 'test-results/screenshots/error-dashboard.png',
        fullPage: true,
      })
    })
  })
})
