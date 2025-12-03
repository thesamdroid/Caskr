/**
 * usePushNotifications Hook
 *
 * React hook for managing push notification state and actions.
 */

import { useState, useEffect, useCallback } from 'react'
import {
  isPushSupported,
  getNotificationPermission,
  requestNotificationPermission,
  subscribeToPush,
  unsubscribeFromPush,
  isSubscribed,
  getNotificationPreferences,
  updateNotificationPreferences,
  sendTestNotification,
  getUserSubscriptions,
  removeSubscription,
  type NotificationPreferences,
  type PushSubscriptionData,
} from './pushSubscription'

export interface PushNotificationState {
  /** Whether push is supported in this browser */
  isSupported: boolean
  /** Current permission status */
  permission: NotificationPermission
  /** Whether currently subscribed */
  subscribed: boolean
  /** Whether loading state */
  isLoading: boolean
  /** Error message if any */
  error: string | null
  /** User's notification preferences */
  preferences: NotificationPreferences | null
  /** User's subscriptions across devices */
  subscriptions: PushSubscriptionData[]
  /** Request notification permission */
  requestPermission: () => Promise<NotificationPermission>
  /** Subscribe to push notifications */
  subscribe: (deviceName?: string) => Promise<boolean>
  /** Unsubscribe from push notifications */
  unsubscribe: () => Promise<boolean>
  /** Update notification preferences */
  updatePreferences: (prefs: Partial<NotificationPreferences>) => Promise<boolean>
  /** Remove a specific subscription */
  removeSubscription: (id: number) => Promise<boolean>
  /** Send a test notification */
  sendTest: () => Promise<boolean>
  /** Refresh state */
  refresh: () => Promise<void>
}

/**
 * Hook for managing push notifications
 */
export function usePushNotifications(): PushNotificationState {
  const [isSupported] = useState(() => isPushSupported())
  const [permission, setPermission] = useState<NotificationPermission>(() =>
    getNotificationPermission()
  )
  const [subscribed, setSubscribed] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [preferences, setPreferences] = useState<NotificationPreferences | null>(null)
  const [subscriptions, setSubscriptions] = useState<PushSubscriptionData[]>([])

  // Load initial state
  const refresh = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      // Check subscription status
      const isSubscribedNow = await isSubscribed()
      setSubscribed(isSubscribedNow)

      // Load preferences
      const prefs = await getNotificationPreferences()
      setPreferences(prefs)

      // Load subscriptions
      const subs = await getUserSubscriptions()
      setSubscriptions(subs)

      // Update permission state
      setPermission(getNotificationPermission())
    } catch (err) {
      setError('Failed to load notification settings')
      console.error('[usePushNotifications] Error:', err)
    } finally {
      setIsLoading(false)
    }
  }, [])

  // Initial load
  useEffect(() => {
    if (isSupported) {
      refresh()
    } else {
      setIsLoading(false)
    }
  }, [isSupported, refresh])

  // Request permission
  const requestPermissionHandler = useCallback(async (): Promise<NotificationPermission> => {
    try {
      const perm = await requestNotificationPermission()
      setPermission(perm)
      return perm
    } catch (err) {
      setError('Failed to request permission')
      return 'denied'
    }
  }, [])

  // Subscribe
  const subscribeHandler = useCallback(async (deviceName?: string): Promise<boolean> => {
    setIsLoading(true)
    setError(null)

    try {
      const result = await subscribeToPush(deviceName)
      if (result) {
        setSubscribed(true)
        await refresh()
        return true
      } else {
        setError('Failed to subscribe to push notifications')
        return false
      }
    } catch (err) {
      setError('Failed to subscribe to push notifications')
      return false
    } finally {
      setIsLoading(false)
    }
  }, [refresh])

  // Unsubscribe
  const unsubscribeHandler = useCallback(async (): Promise<boolean> => {
    setIsLoading(true)
    setError(null)

    try {
      const success = await unsubscribeFromPush()
      if (success) {
        setSubscribed(false)
        await refresh()
        return true
      } else {
        setError('Failed to unsubscribe')
        return false
      }
    } catch (err) {
      setError('Failed to unsubscribe')
      return false
    } finally {
      setIsLoading(false)
    }
  }, [refresh])

  // Update preferences
  const updatePreferencesHandler = useCallback(
    async (prefs: Partial<NotificationPreferences>): Promise<boolean> => {
      setIsLoading(true)
      setError(null)

      try {
        const updated = await updateNotificationPreferences(prefs)
        if (updated) {
          setPreferences(updated)
          return true
        } else {
          setError('Failed to update preferences')
          return false
        }
      } catch (err) {
        setError('Failed to update preferences')
        return false
      } finally {
        setIsLoading(false)
      }
    },
    []
  )

  // Remove subscription
  const removeSubscriptionHandler = useCallback(
    async (id: number): Promise<boolean> => {
      setIsLoading(true)
      setError(null)

      try {
        const success = await removeSubscription(id)
        if (success) {
          await refresh()
          return true
        } else {
          setError('Failed to remove subscription')
          return false
        }
      } catch (err) {
        setError('Failed to remove subscription')
        return false
      } finally {
        setIsLoading(false)
      }
    },
    [refresh]
  )

  // Send test notification
  const sendTestHandler = useCallback(async (): Promise<boolean> => {
    setError(null)

    try {
      const success = await sendTestNotification()
      if (!success) {
        setError('Failed to send test notification')
      }
      return success
    } catch (err) {
      setError('Failed to send test notification')
      return false
    }
  }, [])

  return {
    isSupported,
    permission,
    subscribed,
    isLoading,
    error,
    preferences,
    subscriptions,
    requestPermission: requestPermissionHandler,
    subscribe: subscribeHandler,
    unsubscribe: unsubscribeHandler,
    updatePreferences: updatePreferencesHandler,
    removeSubscription: removeSubscriptionHandler,
    sendTest: sendTestHandler,
    refresh,
  }
}

export default usePushNotifications
