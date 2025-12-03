/**
 * Push Subscription Management
 *
 * Handles Web Push subscription lifecycle including:
 * - Permission requests
 * - Subscribe/unsubscribe
 * - Backend synchronization
 */

import { getRegistration } from '../serviceWorkerRegistration'

// API endpoints
const API_BASE = '/api/push'

export interface PushSubscriptionData {
  id: number
  deviceName?: string
  isActive: boolean
  createdAt: string
  lastUsedAt?: string
}

export interface NotificationPreferences {
  notificationsEnabled: boolean
  taskAssignments: boolean
  taskReminders: boolean
  complianceAlerts: boolean
  syncStatus: boolean
  quietHoursStart?: string
  quietHoursEnd?: string
  timezone?: string
}

/**
 * Check if push notifications are supported
 */
export function isPushSupported(): boolean {
  return (
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  )
}

/**
 * Get current notification permission status
 */
export function getNotificationPermission(): NotificationPermission {
  if (!('Notification' in window)) {
    return 'denied'
  }
  return Notification.permission
}

/**
 * Request notification permission from user
 */
export async function requestNotificationPermission(): Promise<NotificationPermission> {
  if (!('Notification' in window)) {
    return 'denied'
  }

  const permission = await Notification.requestPermission()
  return permission
}

/**
 * Get VAPID public key from server
 */
async function getVapidPublicKey(): Promise<string | null> {
  try {
    const response = await fetch(`${API_BASE}/vapid-public-key`)
    if (!response.ok) {
      return null
    }
    const data = await response.json()
    return data.publicKey
  } catch {
    return null
  }
}

/**
 * Convert URL base64 to Uint8Array (for VAPID key)
 */
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')

  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }

  return outputArray
}

/**
 * Subscribe to push notifications
 */
export async function subscribeToPush(deviceName?: string): Promise<PushSubscriptionData | null> {
  // Check support
  if (!isPushSupported()) {
    console.log('[Push] Push notifications not supported')
    return null
  }

  // Request permission if needed
  const permission = await requestNotificationPermission()
  if (permission !== 'granted') {
    console.log('[Push] Permission denied')
    return null
  }

  // Get VAPID public key
  const vapidKey = await getVapidPublicKey()
  if (!vapidKey) {
    console.error('[Push] VAPID public key not available')
    return null
  }

  // Get service worker registration
  const registration = getRegistration()
  if (!registration) {
    console.error('[Push] Service worker not registered')
    return null
  }

  try {
    // Subscribe with push manager
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidKey),
    })

    // Extract keys
    const subscriptionJson = subscription.toJSON()
    const keys = subscriptionJson.keys!

    // Send to backend
    const response = await fetch(`${API_BASE}/subscribe`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        endpoint: subscriptionJson.endpoint,
        p256dhKey: keys.p256dh,
        authKey: keys.auth,
        deviceName,
      }),
    })

    if (!response.ok) {
      throw new Error('Failed to save subscription')
    }

    const data = await response.json()
    console.log('[Push] Subscribed successfully')
    return data
  } catch (error) {
    console.error('[Push] Subscription failed:', error)
    return null
  }
}

/**
 * Unsubscribe from push notifications
 */
export async function unsubscribeFromPush(): Promise<boolean> {
  const registration = getRegistration()
  if (!registration) {
    return false
  }

  try {
    const subscription = await registration.pushManager.getSubscription()
    if (!subscription) {
      return true // Already unsubscribed
    }

    // Unsubscribe locally
    await subscription.unsubscribe()

    // Remove from backend
    await fetch(`${API_BASE}/subscribe`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        endpoint: subscription.endpoint,
      }),
    })

    console.log('[Push] Unsubscribed successfully')
    return true
  } catch (error) {
    console.error('[Push] Unsubscribe failed:', error)
    return false
  }
}

/**
 * Check if currently subscribed to push
 */
export async function isSubscribed(): Promise<boolean> {
  const registration = getRegistration()
  if (!registration) {
    return false
  }

  try {
    const subscription = await registration.pushManager.getSubscription()
    return subscription !== null
  } catch {
    return false
  }
}

/**
 * Get current subscription
 */
export async function getCurrentSubscription(): Promise<PushSubscription | null> {
  const registration = getRegistration()
  if (!registration) {
    return null
  }

  return registration.pushManager.getSubscription()
}

/**
 * Get all subscriptions for current user from backend
 */
export async function getUserSubscriptions(): Promise<PushSubscriptionData[]> {
  try {
    const response = await fetch(`${API_BASE}/subscriptions`)
    if (!response.ok) {
      return []
    }
    return response.json()
  } catch {
    return []
  }
}

/**
 * Remove a specific subscription by ID
 */
export async function removeSubscription(id: number): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE}/subscriptions/${id}`, {
      method: 'DELETE',
    })
    return response.ok
  } catch {
    return false
  }
}

/**
 * Get notification preferences
 */
export async function getNotificationPreferences(): Promise<NotificationPreferences | null> {
  try {
    const response = await fetch(`${API_BASE}/preferences`)
    if (!response.ok) {
      return null
    }
    return response.json()
  } catch {
    return null
  }
}

/**
 * Update notification preferences
 */
export async function updateNotificationPreferences(
  preferences: Partial<NotificationPreferences>
): Promise<NotificationPreferences | null> {
  try {
    const response = await fetch(`${API_BASE}/preferences`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(preferences),
    })
    if (!response.ok) {
      return null
    }
    return response.json()
  } catch {
    return null
  }
}

/**
 * Send a test notification
 */
export async function sendTestNotification(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE}/test`, {
      method: 'POST',
    })
    return response.ok
  } catch {
    return false
  }
}
