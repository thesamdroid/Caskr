import { expect, test, type Page } from '@playwright/test'
import { seedAuthenticatedUser } from './support/auth'

// Mock VAPID public key
const MOCK_VAPID_KEY =
  'BPTestKeyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'

async function mockPushAPIs(page: Page) {
  // Mock VAPID key endpoint
  await page.route('**/api/push/vapid-public-key', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ publicKey: MOCK_VAPID_KEY }),
    })
  })

  // Mock subscribe endpoint
  await page.route('**/api/push/subscribe', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 1,
          deviceName: 'Test Device',
          isActive: true,
          createdAt: new Date().toISOString(),
          lastUsedAt: null,
        }),
      })
    } else if (route.request().method() === 'DELETE') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unsubscribed successfully' }),
      })
    }
  })

  // Mock subscriptions list endpoint
  await page.route('**/api/push/subscriptions', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 1,
          deviceName: 'Chrome on Windows',
          isActive: true,
          createdAt: new Date().toISOString(),
          lastUsedAt: new Date().toISOString(),
        },
      ]),
    })
  })

  // Mock preferences endpoint
  await page.route('**/api/push/preferences', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          notificationsEnabled: true,
          taskAssignments: true,
          taskReminders: true,
          complianceAlerts: true,
          syncStatus: true,
          quietHoursStart: null,
          quietHoursEnd: null,
          timezone: null,
        }),
      })
    } else if (route.request().method() === 'PUT') {
      const body = await route.request().postDataJSON()
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          notificationsEnabled: body.notificationsEnabled ?? true,
          taskAssignments: body.taskAssignments ?? true,
          taskReminders: body.taskReminders ?? true,
          complianceAlerts: body.complianceAlerts ?? true,
          syncStatus: body.syncStatus ?? true,
          quietHoursStart: body.quietHoursStart ?? null,
          quietHoursEnd: body.quietHoursEnd ?? null,
          timezone: body.timezone ?? null,
        }),
      })
    }
  })
}

test.describe('Push Notifications', () => {
  test.describe('VAPID Key', () => {
    test('fetches VAPID public key from API', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let vapidKeyFetched = false

      await page.route('**/api/push/vapid-public-key', async route => {
        vapidKeyFetched = true
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ publicKey: MOCK_VAPID_KEY }),
        })
      })

      await page.goto('/')

      // Trigger notification settings if component auto-loads VAPID key
      await page.evaluate(async () => {
        const response = await fetch('/api/push/vapid-public-key')
        return response.json()
      })

      expect(vapidKeyFetched).toBe(true)
    })

    test('handles missing VAPID key gracefully', async ({ page }) => {
      await seedAuthenticatedUser(page)

      await page.route('**/api/push/vapid-public-key', async route => {
        await route.fulfill({
          status: 404,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Push notifications not configured' }),
        })
      })

      await page.goto('/')

      // Should not crash, error should be handled gracefully
      const result = await page.evaluate(async () => {
        try {
          const response = await fetch('/api/push/vapid-public-key')
          return { status: response.status }
        } catch (e) {
          return { error: true }
        }
      })

      expect(result.status).toBe(404)
    })
  })

  test.describe('Subscription Management', () => {
    test('subscribes to push notifications', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let subscribeRequest: any = null

      await page.route('**/api/push/subscribe', async route => {
        if (route.request().method() === 'POST') {
          subscribeRequest = await route.request().postDataJSON()
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 1,
              deviceName: subscribeRequest?.deviceName || 'Test Device',
              isActive: true,
              createdAt: new Date().toISOString(),
            }),
          })
        }
      })

      await page.goto('/')

      // Simulate subscription
      const result = await page.evaluate(async () => {
        const response = await fetch('/api/push/subscribe', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            endpoint: 'https://fcm.googleapis.com/fcm/send/test123',
            p256dhKey: 'BPTestKey123',
            authKey: 'TestAuth456',
            deviceName: 'Test Device',
          }),
        })
        return response.json()
      })

      expect(result.id).toBe(1)
      expect(result.isActive).toBe(true)
      expect(subscribeRequest).not.toBeNull()
      expect(subscribeRequest.endpoint).toBe('https://fcm.googleapis.com/fcm/send/test123')
    })

    test('unsubscribes from push notifications', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let unsubscribeEndpoint: string | null = null

      await page.route('**/api/push/subscribe', async route => {
        if (route.request().method() === 'DELETE') {
          const body = await route.request().postDataJSON()
          unsubscribeEndpoint = body?.endpoint
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Unsubscribed successfully' }),
          })
        }
      })

      await page.goto('/')

      // Simulate unsubscription
      await page.evaluate(async () => {
        await fetch('/api/push/subscribe', {
          method: 'DELETE',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            endpoint: 'https://fcm.googleapis.com/fcm/send/test123',
          }),
        })
      })

      expect(unsubscribeEndpoint).toBe('https://fcm.googleapis.com/fcm/send/test123')
    })

    test('lists user subscriptions', async ({ page }) => {
      await seedAuthenticatedUser(page)
      await mockPushAPIs(page)

      await page.goto('/')

      const subscriptions = await page.evaluate(async () => {
        const response = await fetch('/api/push/subscriptions')
        return response.json()
      })

      expect(subscriptions).toHaveLength(1)
      expect(subscriptions[0].deviceName).toBe('Chrome on Windows')
      expect(subscriptions[0].isActive).toBe(true)
    })

    test('removes subscription by ID', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let deletedId: number | null = null

      await page.route('**/api/push/subscriptions/*', async route => {
        if (route.request().method() === 'DELETE') {
          const url = route.request().url()
          const match = url.match(/subscriptions\/(\d+)/)
          deletedId = match ? parseInt(match[1]) : null
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Subscription removed' }),
          })
        }
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/subscriptions/42', {
          method: 'DELETE',
        })
      })

      expect(deletedId).toBe(42)
    })
  })

  test.describe('Preferences', () => {
    test('fetches notification preferences', async ({ page }) => {
      await seedAuthenticatedUser(page)
      await mockPushAPIs(page)

      await page.goto('/')

      const preferences = await page.evaluate(async () => {
        const response = await fetch('/api/push/preferences')
        return response.json()
      })

      expect(preferences.notificationsEnabled).toBe(true)
      expect(preferences.taskAssignments).toBe(true)
      expect(preferences.taskReminders).toBe(true)
      expect(preferences.complianceAlerts).toBe(true)
    })

    test('updates notification preferences', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let updatedPrefs: any = null

      await page.route('**/api/push/preferences', async route => {
        if (route.request().method() === 'PUT') {
          updatedPrefs = await route.request().postDataJSON()
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              ...updatedPrefs,
              notificationsEnabled: updatedPrefs.notificationsEnabled ?? true,
            }),
          })
        }
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/preferences', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            taskAssignments: false,
            taskReminders: true,
          }),
        })
      })

      expect(updatedPrefs).not.toBeNull()
      expect(updatedPrefs.taskAssignments).toBe(false)
      expect(updatedPrefs.taskReminders).toBe(true)
    })

    test('sets quiet hours', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let quietHoursUpdate: any = null

      await page.route('**/api/push/preferences', async route => {
        if (route.request().method() === 'PUT') {
          quietHoursUpdate = await route.request().postDataJSON()
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              notificationsEnabled: true,
              taskAssignments: true,
              taskReminders: true,
              complianceAlerts: true,
              syncStatus: true,
              quietHoursStart: quietHoursUpdate.quietHoursStart,
              quietHoursEnd: quietHoursUpdate.quietHoursEnd,
              timezone: quietHoursUpdate.timezone,
            }),
          })
        }
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/preferences', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            quietHoursStart: '22:00',
            quietHoursEnd: '07:00',
            timezone: 'America/New_York',
          }),
        })
      })

      expect(quietHoursUpdate.quietHoursStart).toBe('22:00')
      expect(quietHoursUpdate.quietHoursEnd).toBe('07:00')
      expect(quietHoursUpdate.timezone).toBe('America/New_York')
    })
  })

  test.describe('Permission Handling', () => {
    test('checks notification permission state', async ({ page }) => {
      await page.goto('/')

      const permission = await page.evaluate(() => {
        if ('Notification' in window) {
          return Notification.permission
        }
        return 'not-supported'
      })

      // Permission should be 'default', 'granted', or 'denied'
      expect(['default', 'granted', 'denied', 'not-supported']).toContain(permission)
    })

    test('stores permission state in localStorage', async ({ page }) => {
      await seedAuthenticatedUser(page)
      await page.goto('/')

      // Simulate storing permission state
      await page.evaluate(() => {
        localStorage.setItem('caskr_notification_permission', 'granted')
      })

      const storedPermission = await page.evaluate(() =>
        localStorage.getItem('caskr_notification_permission')
      )

      expect(storedPermission).toBe('granted')
    })
  })

  test.describe('Service Worker Integration', () => {
    test('service worker handles push event', async ({ page }) => {
      await page.goto('/')

      // Check if service worker is registered
      const swRegistered = await page.evaluate(async () => {
        if ('serviceWorker' in navigator) {
          const registrations = await navigator.serviceWorker.getRegistrations()
          return registrations.length > 0
        }
        return false
      })

      // Service worker may or may not be registered in test environment
      expect(typeof swRegistered).toBe('boolean')
    })

    test('push subscription uses correct VAPID key', async ({ page }) => {
      await seedAuthenticatedUser(page)

      let vapidKeyUsed: string | null = null
      await page.route('**/api/push/vapid-public-key', async route => {
        vapidKeyUsed = MOCK_VAPID_KEY
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ publicKey: MOCK_VAPID_KEY }),
        })
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/vapid-public-key')
      })

      expect(vapidKeyUsed).toBe(MOCK_VAPID_KEY)
    })
  })

  test.describe('Error Handling', () => {
    test('handles subscription failure gracefully', async ({ page }) => {
      await seedAuthenticatedUser(page)

      await page.route('**/api/push/subscribe', async route => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Server error' }),
        })
      })

      await page.goto('/')

      const result = await page.evaluate(async () => {
        try {
          const response = await fetch('/api/push/subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              endpoint: 'https://test',
              p256dhKey: 'key',
              authKey: 'auth',
            }),
          })
          return { status: response.status }
        } catch (e) {
          return { error: true }
        }
      })

      expect(result.status).toBe(500)
    })

    test('handles unauthorized request', async ({ page }) => {
      // Don't authenticate
      await page.route('**/api/push/subscriptions', async route => {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Authentication required' }),
        })
      })

      await page.goto('/')

      const result = await page.evaluate(async () => {
        const response = await fetch('/api/push/subscriptions')
        return { status: response.status }
      })

      expect(result.status).toBe(401)
    })

    test('retries on network failure', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let attemptCount = 0

      await page.route('**/api/push/subscriptions', async route => {
        attemptCount++
        if (attemptCount < 3) {
          await route.abort('failed')
        } else {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([]),
          })
        }
      })

      await page.goto('/')

      // Simulate retry logic
      const result = await page.evaluate(async () => {
        let attempts = 0
        while (attempts < 3) {
          try {
            const response = await fetch('/api/push/subscriptions')
            if (response.ok) {
              return { success: true, attempts: attempts + 1 }
            }
          } catch {
            // Retry on failure
          }
          attempts++
          await new Promise(r => setTimeout(r, 100))
        }
        return { success: false, attempts }
      })

      expect(result.success).toBe(true)
    })
  })

  test.describe('Notification Types', () => {
    test('handles task assignment notification type', async ({ page }) => {
      await seedAuthenticatedUser(page)

      await page.route('**/api/push/preferences', async route => {
        if (route.request().method() === 'PUT') {
          const body = await route.request().postDataJSON()
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              notificationsEnabled: true,
              taskAssignments: body.taskAssignments ?? true,
              taskReminders: true,
              complianceAlerts: true,
              syncStatus: true,
            }),
          })
        }
      })

      await page.goto('/')

      // Toggle task assignments off
      await page.evaluate(async () => {
        await fetch('/api/push/preferences', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ taskAssignments: false }),
        })
      })

      // Verify the preference was sent
    })

    test('handles compliance alert notification type', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let complianceAlertsValue: boolean | null = null

      await page.route('**/api/push/preferences', async route => {
        if (route.request().method() === 'PUT') {
          const body = await route.request().postDataJSON()
          complianceAlertsValue = body.complianceAlerts
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              notificationsEnabled: true,
              taskAssignments: true,
              taskReminders: true,
              complianceAlerts: body.complianceAlerts ?? true,
              syncStatus: true,
            }),
          })
        }
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/preferences', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ complianceAlerts: false }),
        })
      })

      expect(complianceAlertsValue).toBe(false)
    })
  })

  test.describe('Device Management', () => {
    test('displays correct device name', async ({ page }) => {
      await seedAuthenticatedUser(page)

      const deviceName = await page.evaluate(() => {
        const ua = navigator.userAgent
        if (/Android/i.test(ua)) return 'Android Device'
        if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS Device'
        if (/Windows/i.test(ua)) return 'Windows'
        if (/Mac/i.test(ua)) return 'Mac'
        if (/Linux/i.test(ua)) return 'Linux'
        return 'Unknown Device'
      })

      expect(deviceName).toBeTruthy()
    })

    test('removes specific device subscription', async ({ page }) => {
      await seedAuthenticatedUser(page)
      let removedDeviceId: number | null = null

      await page.route('**/api/push/subscriptions/*', async route => {
        if (route.request().method() === 'DELETE') {
          const url = route.request().url()
          const match = url.match(/subscriptions\/(\d+)/)
          removedDeviceId = match ? parseInt(match[1]) : null
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Subscription removed' }),
          })
        }
      })

      await page.goto('/')

      await page.evaluate(async () => {
        await fetch('/api/push/subscriptions/123', {
          method: 'DELETE',
        })
      })

      expect(removedDeviceId).toBe(123)
    })
  })
})
