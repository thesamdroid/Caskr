/**
 * Background Sync Service
 *
 * Handles queued offline actions and syncs when online.
 */

import {
  getSyncQueue,
  removeSyncItem,
  updateSyncItemRetries,
  getSyncQueueCount,
} from '../storage/offlineStorage'

type SyncStatusCallback = (status: SyncStatus) => void

export interface SyncStatus {
  isProcessing: boolean
  pendingCount: number
  lastSyncTime: number | null
  lastError: string | null
}

let isProcessing = false
let statusCallback: SyncStatusCallback | null = null
let lastSyncTime: number | null = null
let lastError: string | null = null

/**
 * Process the sync queue
 */
export async function processSyncQueue(): Promise<void> {
  if (isProcessing || !navigator.onLine) {
    return
  }

  isProcessing = true
  notifyStatus()

  try {
    const queue = await getSyncQueue()

    for (const item of queue) {
      try {
        const response = await fetch(item.url, {
          method: item.method,
          headers: {
            ...item.headers,
            'Content-Type': 'application/json',
          },
          body: item.body,
        })

        if (response.ok) {
          await removeSyncItem(item.id)
          console.log(`[BackgroundSync] Synced: ${item.method} ${item.url}`)
        } else if (response.status >= 400 && response.status < 500) {
          // Client error - don't retry
          console.error(`[BackgroundSync] Client error ${response.status}, removing item`)
          await removeSyncItem(item.id)
        } else {
          // Server error - increment retry count
          if (item.retries >= item.maxRetries) {
            console.error(`[BackgroundSync] Max retries reached, removing item`)
            await removeSyncItem(item.id)
          } else {
            await updateSyncItemRetries(item.id, item.retries + 1)
          }
        }
      } catch (err) {
        console.error(`[BackgroundSync] Network error:`, err)

        if (item.retries >= item.maxRetries) {
          await removeSyncItem(item.id)
          lastError = 'Max retries reached for some items'
        } else {
          await updateSyncItemRetries(item.id, item.retries + 1)
        }
      }
    }

    lastSyncTime = Date.now()
    lastError = null
  } catch (err) {
    console.error('[BackgroundSync] Queue processing error:', err)
    lastError = err instanceof Error ? err.message : 'Sync failed'
  } finally {
    isProcessing = false
    notifyStatus()
  }
}

/**
 * Register the sync service
 */
export function registerSyncService(): void {
  // Process queue when coming online
  window.addEventListener('online', () => {
    console.log('[BackgroundSync] Online - processing queue')
    processSyncQueue()
  })

  // Initial processing if online
  if (navigator.onLine) {
    processSyncQueue()
  }

  // Register Background Sync API if available
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.ready.then((registration) => {
      // Check if sync is available on this registration
      const reg = registration as ServiceWorkerRegistration & { sync?: SyncManager }
      if (reg.sync) {
        // Background Sync tag
        reg.sync.register('caskr-sync').catch((err) => {
          console.log('[BackgroundSync] Background Sync registration failed:', err)
        })
      }
    })
  }

  // Periodic sync as fallback
  setInterval(() => {
    if (navigator.onLine) {
      processSyncQueue()
    }
  }, 30000) // Every 30 seconds
}

/**
 * Subscribe to sync status updates
 */
export function subscribeToSyncStatus(callback: SyncStatusCallback): () => void {
  statusCallback = callback
  notifyStatus()

  return () => {
    statusCallback = null
  }
}

/**
 * Notify status subscribers
 */
async function notifyStatus(): Promise<void> {
  if (!statusCallback) return

  const pendingCount = await getSyncQueueCount()

  statusCallback({
    isProcessing,
    pendingCount,
    lastSyncTime,
    lastError,
  })
}

/**
 * Get current sync status
 */
export async function getSyncStatus(): Promise<SyncStatus> {
  const pendingCount = await getSyncQueueCount()

  return {
    isProcessing,
    pendingCount,
    lastSyncTime,
    lastError,
  }
}

/**
 * Force sync immediately
 */
export async function forceSync(): Promise<void> {
  if (!navigator.onLine) {
    throw new Error('Cannot sync while offline')
  }

  await processSyncQueue()
}

/**
 * Clear the sync queue
 */
export async function clearSyncQueue(): Promise<void> {
  const queue = await getSyncQueue()
  for (const item of queue) {
    await removeSyncItem(item.id)
  }
  notifyStatus()
}

// SyncManager type declaration for TypeScript
interface SyncManager {
  register(tag: string): Promise<void>
  getTags(): Promise<string[]>
}

export default {
  processSyncQueue,
  registerSyncService,
  subscribeToSyncStatus,
  getSyncStatus,
  forceSync,
  clearSyncQueue,
}
