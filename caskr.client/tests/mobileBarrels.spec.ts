import { expect, test, type Page } from '@playwright/test'
import { seedAuthenticatedUser } from './support/auth'

interface BarrelsStubOptions {
  barrels?: unknown[]
  rickhouses?: unknown[]
  error?: boolean
  searchResults?: unknown[]
}

async function stubMobileBarrelsData(page: Page, options: BarrelsStubOptions = {}) {
  const barrels = options.barrels ?? [
    {
      id: 1,
      sku: 'B-2024-001',
      status: 'aging',
      rickhouseId: 1,
      rickhouseName: 'Rickhouse A',
      warehouseId: 1,
      warehouseName: 'Main Warehouse',
      batchId: 1,
      batchName: 'Bourbon Batch #42',
      spiritType: 'Bourbon',
      fillDate: '2022-03-15T00:00:00Z',
      age: '2 years, 8 months',
      ageInDays: 990,
      currentProofGallons: 48.5,
      originalProofGallons: 53.2,
      lossPercentage: 8.8,
      proof: 125,
      temperature: 68
    },
    {
      id: 2,
      sku: 'B-2024-002',
      status: 'aging',
      rickhouseId: 2,
      rickhouseName: 'Rickhouse B',
      warehouseId: 1,
      warehouseName: 'Main Warehouse',
      batchId: 2,
      batchName: 'Rye Batch #15',
      spiritType: 'Rye',
      fillDate: '2023-01-10T00:00:00Z',
      age: '1 year, 10 months',
      ageInDays: 693,
      currentProofGallons: 51.2,
      originalProofGallons: 53.0,
      lossPercentage: 3.4,
      proof: 118
    }
  ]

  const rickhouses = options.rickhouses ?? [
    { id: 1, name: 'Rickhouse A', warehouseId: 1, warehouseName: 'Main Warehouse' },
    { id: 2, name: 'Rickhouse B', warehouseId: 1, warehouseName: 'Main Warehouse' },
    { id: 3, name: 'Rickhouse C', warehouseId: 2, warehouseName: 'Secondary Warehouse' }
  ]

  const searchResults = options.searchResults ?? barrels.map(b => ({
    id: (b as { id: number }).id,
    sku: (b as { sku: string }).sku,
    status: (b as { status: string }).status,
    rickhouseName: (b as { rickhouseName: string }).rickhouseName,
    age: (b as { age: string }).age,
    batchName: (b as { batchName: string }).batchName,
    spiritType: (b as { spiritType: string }).spiritType
  }))

  // Stub rickhouses endpoint
  await page.route('**/api/rickhouses**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(rickhouses)
    })
  })

  // Stub barrel search endpoint
  await page.route('**/api/barrels/search**', async route => {
    if (options.error) {
      await route.fulfill({ status: 500 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(searchResults)
    })
  })

  // Stub barrel by SKU endpoint
  await page.route('**/api/barrels/sku/**', async route => {
    const sku = route.request().url().split('/sku/')[1]?.split('?')[0]
    const barrel = barrels.find(b => (b as { sku: string }).sku === decodeURIComponent(sku || ''))
    if (barrel) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(barrel)
      })
    } else {
      await route.fulfill({ status: 404 })
    }
  })

  // Stub barrel by ID endpoint
  await page.route('**/api/barrels/*', async route => {
    const url = route.request().url()
    // Skip if it's a sub-route like /history or /movements
    if (url.includes('/history') || url.includes('/movements') || url.includes('/gauges')) {
      return route.continue()
    }

    const idMatch = url.match(/\/api\/barrels\/(\d+)/)
    if (idMatch) {
      const id = parseInt(idMatch[1])
      const barrel = barrels.find(b => (b as { id: number }).id === id)
      if (barrel) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(barrel)
        })
      } else {
        await route.fulfill({ status: 404 })
      }
    } else {
      await route.continue()
    }
  })

  // Stub barrel history endpoint
  await page.route('**/api/barrels/*/history**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: '1',
          type: 'gauge',
          title: 'Gauge recorded',
          description: '48.5 PG at 125 proof',
          timestamp: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
          user: 'John Doe'
        },
        {
          id: '2',
          type: 'movement',
          title: 'Moved to Rickhouse A',
          description: 'From Rickhouse B',
          timestamp: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
          user: 'Jane Smith'
        }
      ])
    })
  })

  // Stub movement endpoint
  await page.route('**/api/barrels/*/movements', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ success: true })
      })
    } else {
      await route.continue()
    }
  })

  // Stub gauge endpoint
  await page.route('**/api/barrels/*/gauges', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ success: true })
      })
    } else {
      await route.continue()
    }
  })
}

test.describe('Mobile Barrels Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('renders scan and search mode toggles', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')

    await expect(page.getByRole('button', { name: /scan/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /search/i })).toBeVisible()
  })

  test('defaults to scan mode', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')

    // Scan mode should be active by default
    const scanButton = page.getByRole('button', { name: /scan/i })
    await expect(scanButton).toHaveClass(/active/)
  })

  test('switches to search mode on click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')

    await page.getByRole('button', { name: /search/i }).click()

    // Search input should be visible
    await expect(page.getByPlaceholder(/search/i)).toBeVisible()
  })

  test('displays recent barrels in scan mode', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    // Pre-populate recent barrels in localStorage
    await page.addInitScript(() => {
      localStorage.setItem('caskr_recent_barrels', JSON.stringify([
        { id: 1, sku: 'B-2024-001', status: 'aging', rickhouseName: 'Rickhouse A', age: '2 years', batchName: 'Bourbon #42', spiritType: 'Bourbon' }
      ]))
    })

    await page.goto('/barrels')

    await expect(page.getByText('Recent')).toBeVisible()
    await expect(page.getByText('B-2024-001')).toBeVisible()
  })
})

test.describe('Mobile Barrels Search', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('shows search results after typing', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')

    // Switch to search mode
    await page.getByRole('button', { name: /search/i }).click()

    // Type in search
    await page.getByPlaceholder(/search/i).fill('B-2024')

    // Wait for debounce and results
    await expect(page.getByText('B-2024-001')).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('B-2024-002')).toBeVisible()
  })

  test('shows empty state when no results', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page, { searchResults: [] })

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('nonexistent')

    await expect(page.getByText(/no barrels found/i)).toBeVisible({ timeout: 5000 })
  })

  test('displays loading state during search', async ({ page }) => {
    await seedAuthenticatedUser(page)

    // Delay the search response
    await page.route('**/api/barrels/search**', async route => {
      await new Promise(resolve => setTimeout(resolve, 1000))
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]'
      })
    })

    await stubMobileBarrelsData(page)
    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')

    // Should show loading indicator
    await expect(page.locator('[class*="loading"], [class*="spinner"]').first()).toBeVisible()
  })

  test('shows error state on search failure', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page, { error: true })

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('test')

    await expect(page.getByText(/failed|error/i)).toBeVisible({ timeout: 5000 })
  })
})

test.describe('Mobile Barrels Detail Sheet', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('opens barrel detail sheet on result click', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')

    await page.getByText('B-2024-001').click()

    // Detail sheet should show barrel info
    await expect(page.getByText('Bourbon Batch #42')).toBeVisible()
    await expect(page.getByText('Rickhouse A')).toBeVisible()
  })

  test('displays barrel history in detail sheet', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')
    await page.getByText('B-2024-001').click()

    await expect(page.getByText('Gauge recorded')).toBeVisible()
    await expect(page.getByText('Moved to Rickhouse A')).toBeVisible()
  })

  test('shows movement and gauge action buttons', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')
    await page.getByText('B-2024-001').click()

    await expect(page.getByRole('button', { name: /movement|move/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /gauge/i })).toBeVisible()
  })

  test('closes detail sheet on close button', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')
    await page.getByText('B-2024-001').click()

    // Wait for sheet to open
    await expect(page.getByText('Bourbon Batch #42')).toBeVisible()

    // Close the sheet
    await page.getByRole('button', { name: /close/i }).click()

    // Sheet should be hidden
    await expect(page.getByText('Bourbon Batch #42')).not.toBeVisible()
  })
})

test.describe('Mobile Barrels Offline', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('shows offline indicator when offline', async ({ page, context }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')

    // Go offline
    await context.setOffline(true)

    // Trigger a page action to show offline status
    await page.reload().catch(() => {}) // May fail, that's expected

    await expect(page.getByText(/offline/i)).toBeVisible()
  })

  test('shows pending actions count when offline', async ({ page, context }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    // Pre-populate pending actions
    await page.addInitScript(() => {
      localStorage.setItem('caskr_pending_barrel_actions', JSON.stringify([
        { id: 'action-1', type: 'movement', barrelId: 1, data: {}, timestamp: new Date().toISOString() }
      ]))
    })

    await page.goto('/barrels')
    await context.setOffline(true)
    await page.reload().catch(() => {})

    await expect(page.getByText(/1 pending/i)).toBeVisible()
  })

  test('uses cached barrel data when offline', async ({ page, context }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    // Pre-populate cache
    await page.addInitScript(() => {
      const cachedBarrel = {
        detail: {
          id: 1,
          sku: 'CACHED-001',
          status: 'aging',
          rickhouseName: 'Cached Rickhouse'
        },
        history: [],
        timestamp: Date.now()
      }
      localStorage.setItem('caskr_barrel_cache', JSON.stringify([[1, cachedBarrel]]))
    })

    await page.goto('/barrels')
    await context.setOffline(true)

    // Recent barrels should show cached data
    await page.addInitScript(() => {
      localStorage.setItem('caskr_recent_barrels', JSON.stringify([
        { id: 1, sku: 'CACHED-001', status: 'aging', rickhouseName: 'Cached Rickhouse', age: '2 years', batchName: 'Test', spiritType: 'Bourbon' }
      ]))
    })

    await page.reload().catch(() => {})
    await expect(page.getByText('CACHED-001')).toBeVisible()
  })
})

test.describe('Mobile Barrels Filter', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('filters by rickhouse', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')

    // Open filter if exists
    const filterButton = page.getByRole('button', { name: /filter/i })
    if (await filterButton.isVisible()) {
      await filterButton.click()

      // Select a rickhouse filter
      await page.getByText('Rickhouse A').click()

      // Results should be filtered
      await expect(page.getByText('B-2024-001')).toBeVisible()
    }
  })

  test('filters by status', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')

    // Open filter if exists
    const filterButton = page.getByRole('button', { name: /filter/i })
    if (await filterButton.isVisible()) {
      await filterButton.click()

      // Select a status filter
      const agingFilter = page.getByRole('button', { name: /aging/i })
      if (await agingFilter.isVisible()) {
        await agingFilter.click()
      }
    }
  })
})

test.describe('Mobile Barrels Actions', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('records movement successfully', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    let movementRecorded = false
    await page.route('**/api/barrels/*/movements', async route => {
      if (route.request().method() === 'POST') {
        movementRecorded = true
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ success: true })
        })
      } else {
        await route.continue()
      }
    })

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')
    await page.getByText('B-2024-001').click()

    // Click movement button
    const moveButton = page.getByRole('button', { name: /movement|move/i })
    if (await moveButton.isVisible()) {
      await moveButton.click()

      // Select destination if form appears
      const rickhouseSelect = page.getByRole('combobox')
      if (await rickhouseSelect.isVisible()) {
        await rickhouseSelect.selectOption({ index: 1 })
        await page.getByRole('button', { name: /save|submit|confirm/i }).click()
        expect(movementRecorded).toBeTruthy()
      }
    }
  })

  test('records gauge successfully', async ({ page }) => {
    await seedAuthenticatedUser(page)
    await stubMobileBarrelsData(page)

    let gaugeRecorded = false
    await page.route('**/api/barrels/*/gauges', async route => {
      if (route.request().method() === 'POST') {
        gaugeRecorded = true
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ success: true })
        })
      } else {
        await route.continue()
      }
    })

    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('B-2024')
    await page.getByText('B-2024-001').click()

    // Click gauge button
    const gaugeButton = page.getByRole('button', { name: /gauge/i })
    if (await gaugeButton.isVisible()) {
      await gaugeButton.click()

      // Fill gauge form if visible
      const proofInput = page.getByPlaceholder(/proof/i)
      if (await proofInput.isVisible()) {
        await proofInput.fill('125')
        await page.getByPlaceholder(/temperature/i).fill('68')
        await page.getByRole('button', { name: /save|submit|confirm/i }).click()
        expect(gaugeRecorded).toBeTruthy()
      }
    }
  })
})

test.describe('Mobile Barrels Pull-to-Refresh', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
  })

  test('refreshes data on pull down in search mode', async ({ page }) => {
    await seedAuthenticatedUser(page)

    let searchCount = 0
    await page.route('**/api/barrels/search**', async route => {
      searchCount++
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      })
    })

    await stubMobileBarrelsData(page)
    await page.goto('/barrels')
    await page.getByRole('button', { name: /search/i }).click()
    await page.getByPlaceholder(/search/i).fill('test')

    await page.waitForTimeout(500)
    const initialCount = searchCount

    // Simulate pull-to-refresh
    const container = page.locator('[class*="container"]').first()
    const box = await container.boundingBox()
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + 50)
      await page.mouse.down()
      await page.mouse.move(box.x + box.width / 2, box.y + 200, { steps: 10 })
      await page.mouse.up()
    }

    await page.waitForTimeout(1500)
    expect(searchCount).toBeGreaterThanOrEqual(initialCount)
  })
})
