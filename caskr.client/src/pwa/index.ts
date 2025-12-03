/**
 * PWA Module
 *
 * Exports all PWA-related functionality.
 */

// Service Worker
export {
  registerServiceWorker,
  unregisterServiceWorker,
  skipWaiting,
  clearAllCaches,
  checkForUpdates,
  getRegistration,
  isServiceWorkerActive,
  requestNotificationPermission,
  isInstallable,
  listenForInstallPrompt,
  promptInstall,
  isRunningAsPWA,
} from './serviceWorkerRegistration'

// Storage
export {
  cacheApiResponse,
  getCachedResponse,
  invalidateCache,
  clearApiCache,
  addToSyncQueue,
  getSyncQueue,
  cacheEntities,
  getCachedEntities,
  getCachedEntity,
  clearEntityCache,
  isIndexedDBAvailable,
  getStorageEstimate,
  STORES,
} from './storage'

// Background Sync
export {
  processSyncQueue,
  registerSyncService,
  subscribeToSyncStatus,
  getSyncStatus,
  forceSync,
  clearSyncQueue,
} from './sync'
export type { SyncStatus } from './sync'

// Components
export { OfflineIndicator, InstallPrompt } from './components'
export type { OfflineIndicatorProps, InstallPromptProps } from './components'

// Hooks
export { useInstallPrompt } from './hooks'
export type { InstallPromptState } from './hooks'

// Analytics
export {
  trackInstallEvent,
  getStoredEvents,
  clearStoredEvents,
  getInstallFunnelSummary,
  getConversionRate,
  detectPlatform,
} from './analytics'
export type { InstallEvent, InstallEventData } from './analytics'

/**
 * Initialize PWA features
 */
export async function initializePWA(): Promise<void> {
  const { registerServiceWorker, listenForInstallPrompt } = await import(
    './serviceWorkerRegistration'
  )
  const { registerSyncService } = await import('./sync/backgroundSync')

  // Register service worker
  await registerServiceWorker({
    onUpdate: (registration) => {
      console.log('[PWA] Update available')
      // Dispatch custom event for UI to handle
      window.dispatchEvent(
        new CustomEvent('sw-update-available', { detail: { registration } })
      )
    },
    onReady: () => {
      console.log('[PWA] Service worker ready')
    },
    onError: (error) => {
      console.error('[PWA] Service worker error:', error)
    },
  })

  // Listen for install prompt
  listenForInstallPrompt()

  // Register background sync
  registerSyncService()

  console.log('[PWA] Initialized')
}
