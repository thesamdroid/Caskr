import { expect, test } from '@playwright/test'
import * as fs from 'fs'
import * as path from 'path'

test.describe('PWA Manifest', () => {
  test('manifest.json is valid JSON with all required fields', async ({ page }) => {
    const manifestPath = path.join(__dirname, '../public/manifest.json')
    const manifestContent = fs.readFileSync(manifestPath, 'utf-8')

    // Should parse as valid JSON
    const manifest = JSON.parse(manifestContent)

    // Required fields
    expect(manifest.name).toBe('Caskr - Distillery Management')
    expect(manifest.short_name).toBe('Caskr')
    expect(manifest.description).toBeTruthy()
    expect(manifest.start_url).toBe('/?source=pwa')
    expect(manifest.display).toBe('standalone')
    expect(manifest.orientation).toBe('portrait-primary')
    expect(manifest.background_color).toBe('#ffffff')
    expect(manifest.theme_color).toBe('#2563eb')
    expect(manifest.scope).toBe('/')
  })

  test('manifest has correct icon declarations', async () => {
    const manifestPath = path.join(__dirname, '../public/manifest.json')
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'))

    expect(manifest.icons).toBeDefined()
    expect(Array.isArray(manifest.icons)).toBe(true)

    // Check for standard sizes
    const sizes = manifest.icons.map((i: { sizes: string }) => i.sizes)
    expect(sizes).toContain('72x72')
    expect(sizes).toContain('96x96')
    expect(sizes).toContain('128x128')
    expect(sizes).toContain('144x144')
    expect(sizes).toContain('152x152')
    expect(sizes).toContain('192x192')
    expect(sizes).toContain('384x384')
    expect(sizes).toContain('512x512')

    // Check for maskable icons
    const maskableIcons = manifest.icons.filter((i: { purpose: string }) =>
      i.purpose === 'maskable'
    )
    expect(maskableIcons.length).toBeGreaterThanOrEqual(2)
  })

  test('all icon files exist', async () => {
    const iconsDir = path.join(__dirname, '../public/icons')

    const requiredIcons = [
      'icon-72x72.png',
      'icon-96x96.png',
      'icon-128x128.png',
      'icon-144x144.png',
      'icon-152x152.png',
      'icon-192x192.png',
      'icon-192x192-maskable.png',
      'icon-384x384.png',
      'icon-512x512.png',
      'icon-512x512-maskable.png',
      'apple-touch-icon.png',
      'shortcut-scan.png',
      'shortcut-tasks.png',
    ]

    for (const icon of requiredIcons) {
      const iconPath = path.join(iconsDir, icon)
      expect(fs.existsSync(iconPath), `Icon ${icon} should exist`).toBe(true)
    }
  })

  test('manifest has app shortcuts', async () => {
    const manifestPath = path.join(__dirname, '../public/manifest.json')
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'))

    expect(manifest.shortcuts).toBeDefined()
    expect(Array.isArray(manifest.shortcuts)).toBe(true)
    expect(manifest.shortcuts.length).toBeGreaterThanOrEqual(2)

    // Check for Scan Barrel shortcut
    const scanShortcut = manifest.shortcuts.find(
      (s: { name: string }) => s.name === 'Scan Barrel'
    )
    expect(scanShortcut).toBeDefined()
    expect(scanShortcut.url).toContain('/barrels')
    expect(scanShortcut.url).toContain('mode=scan')

    // Check for My Tasks shortcut
    const tasksShortcut = manifest.shortcuts.find(
      (s: { name: string }) => s.name === 'My Tasks'
    )
    expect(tasksShortcut).toBeDefined()
    expect(tasksShortcut.url).toContain('/tasks')
    expect(tasksShortcut.url).toContain('filter=mine')
  })

  test('manifest has screenshots for install UI', async () => {
    const manifestPath = path.join(__dirname, '../public/manifest.json')
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'))

    expect(manifest.screenshots).toBeDefined()
    expect(Array.isArray(manifest.screenshots)).toBe(true)
    expect(manifest.screenshots.length).toBeGreaterThanOrEqual(1)

    // Check screenshot has required fields
    const screenshot = manifest.screenshots[0]
    expect(screenshot.src).toBeTruthy()
    expect(screenshot.sizes).toBeTruthy()
    expect(screenshot.type).toBe('image/png')
    expect(screenshot.form_factor).toBe('narrow')
  })

  test('iOS splash screens exist', async () => {
    const splashDir = path.join(__dirname, '../public/splash')

    // Check a few key splash screen sizes
    const requiredSplash = [
      'apple-splash-1170-2532.png', // iPhone 12/13/14
      'apple-splash-1284-2778.png', // iPhone 12/13 Pro Max
      'apple-splash-750-1334.png', // iPhone 6/7/8
      'apple-splash-2048-2732.png', // iPad Pro 12.9
    ]

    for (const splash of requiredSplash) {
      const splashPath = path.join(splashDir, splash)
      expect(fs.existsSync(splashPath), `Splash ${splash} should exist`).toBe(true)
    }
  })
})

test.describe('PWA Meta Tags in HTML', () => {
  test('index.html has all required PWA meta tags', async ({ page }) => {
    await page.goto('/')

    // Check manifest link
    const manifestLink = await page.locator('link[rel="manifest"]')
    await expect(manifestLink).toHaveAttribute('href', '/manifest.json')

    // Check theme-color
    const themeColor = await page.locator('meta[name="theme-color"]').first()
    await expect(themeColor).toHaveAttribute('content')

    // Check apple-mobile-web-app-capable
    const appleMobile = await page.locator('meta[name="apple-mobile-web-app-capable"]')
    await expect(appleMobile).toHaveAttribute('content', 'yes')

    // Check apple-mobile-web-app-status-bar-style
    const statusBar = await page.locator('meta[name="apple-mobile-web-app-status-bar-style"]')
    await expect(statusBar).toHaveAttribute('content', 'black-translucent')

    // Check apple touch icon
    const touchIcon = await page.locator('link[rel="apple-touch-icon"]').first()
    expect(touchIcon).toBeTruthy()
  })

  test('index.html has iOS splash screen links', async ({ page }) => {
    await page.goto('/')

    // Check for splash screen links
    const splashLinks = await page.locator('link[rel="apple-touch-startup-image"]').all()
    expect(splashLinks.length).toBeGreaterThan(0)

    // Verify splash links have media queries
    const firstSplash = splashLinks[0]
    await expect(firstSplash).toHaveAttribute('media')
    await expect(firstSplash).toHaveAttribute('href')
  })
})
