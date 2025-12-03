/**
 * Service Worker Registration
 *
 * Handles service worker lifecycle and update notifications.
 */

type UpdateCallback = (registration: ServiceWorkerRegistration) => void
type ReadyCallback = (registration: ServiceWorkerRegistration) => void
type ErrorCallback = (error: Error) => void

interface RegistrationConfig {
  onUpdate?: UpdateCallback
  onReady?: ReadyCallback
  onError?: ErrorCallback
}

let swRegistration: ServiceWorkerRegistration | null = null
let updateCallback: UpdateCallback | null = null

/**
 * Register the service worker
 */
export async function registerServiceWorker(config?: RegistrationConfig): Promise<void> {
  if (!('serviceWorker' in navigator)) {
    console.log('[SW] Service workers not supported')
    return
  }

  try {
    const registration = await navigator.serviceWorker.register('/sw.js', {
      scope: '/',
    })

    swRegistration = registration
    console.log('[SW] Service worker registered')

    // Handle registration ready
    if (config?.onReady) {
      config.onReady(registration)
    }

    // Store update callback
    if (config?.onUpdate) {
      updateCallback = config.onUpdate
    }

    // Check for updates
    registration.addEventListener('updatefound', () => {
      const newWorker = registration.installing

      if (newWorker) {
        newWorker.addEventListener('statechange', () => {
          if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
            // New content available
            console.log('[SW] New content available')
            if (updateCallback) {
              updateCallback(registration)
            }
          }
        })
      }
    })

    // Handle controller change (after skipWaiting)
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      console.log('[SW] Controller changed')
    })

    // Handle messages from service worker
    navigator.serviceWorker.addEventListener('message', handleServiceWorkerMessage)

    // Check for existing update
    if (registration.waiting) {
      console.log('[SW] Update waiting')
      if (updateCallback) {
        updateCallback(registration)
      }
    }
  } catch (error) {
    console.error('[SW] Registration failed:', error)
    if (config?.onError) {
      config.onError(error as Error)
    }
  }
}

/**
 * Unregister the service worker
 */
export async function unregisterServiceWorker(): Promise<void> {
  if (!('serviceWorker' in navigator)) {
    return
  }

  try {
    const registration = await navigator.serviceWorker.ready
    await registration.unregister()
    console.log('[SW] Service worker unregistered')
  } catch (error) {
    console.error('[SW] Unregister failed:', error)
  }
}

/**
 * Skip waiting and activate new service worker
 */
export function skipWaiting(): void {
  if (swRegistration?.waiting) {
    swRegistration.waiting.postMessage({ type: 'SKIP_WAITING' })
  }
}

/**
 * Clear all caches
 */
export async function clearAllCaches(): Promise<void> {
  if (swRegistration) {
    swRegistration.active?.postMessage({ type: 'CLEAR_CACHE' })
  }

  // Also clear from main thread
  const cacheNames = await caches.keys()
  await Promise.all(cacheNames.map((name) => caches.delete(name)))
  console.log('[SW] Caches cleared')
}

/**
 * Check for service worker updates
 */
export async function checkForUpdates(): Promise<void> {
  if (swRegistration) {
    await swRegistration.update()
    console.log('[SW] Update check completed')
  }
}

/**
 * Get current service worker registration
 */
export function getRegistration(): ServiceWorkerRegistration | null {
  return swRegistration
}

/**
 * Check if service worker is active
 */
export function isServiceWorkerActive(): boolean {
  return navigator.serviceWorker?.controller !== null
}

/**
 * Handle messages from service worker
 */
function handleServiceWorkerMessage(event: MessageEvent): void {
  const { type, data } = event.data

  switch (type) {
    case 'SYNC_REQUESTED':
      // Trigger background sync processing
      window.dispatchEvent(new CustomEvent('sw-sync-requested', { detail: data }))
      break

    case 'CACHE_UPDATED':
      window.dispatchEvent(new CustomEvent('sw-cache-updated', { detail: data }))
      break

    default:
      console.log('[SW] Unknown message:', type)
  }
}

/**
 * Request notification permission
 */
export async function requestNotificationPermission(): Promise<NotificationPermission> {
  if (!('Notification' in window)) {
    console.log('[SW] Notifications not supported')
    return 'denied'
  }

  const permission = await Notification.requestPermission()
  console.log('[SW] Notification permission:', permission)
  return permission
}

/**
 * Check if app can be installed (PWA)
 */
export function isInstallable(): boolean {
  return 'BeforeInstallPromptEvent' in window
}

// BeforeInstallPromptEvent type
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

let deferredPrompt: BeforeInstallPromptEvent | null = null

/**
 * Listen for install prompt
 */
export function listenForInstallPrompt(): void {
  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault()
    deferredPrompt = e as BeforeInstallPromptEvent
    window.dispatchEvent(new CustomEvent('pwa-installable'))
  })

  window.addEventListener('appinstalled', () => {
    deferredPrompt = null
    window.dispatchEvent(new CustomEvent('pwa-installed'))
  })
}

/**
 * Prompt user to install PWA
 */
export async function promptInstall(): Promise<boolean> {
  if (!deferredPrompt) {
    return false
  }

  await deferredPrompt.prompt()
  const { outcome } = await deferredPrompt.userChoice
  deferredPrompt = null

  return outcome === 'accepted'
}

/**
 * Check if running as installed PWA
 */
export function isRunningAsPWA(): boolean {
  return (
    window.matchMedia('(display-mode: standalone)').matches ||
    (window.navigator as Navigator & { standalone?: boolean }).standalone === true
  )
}

export default {
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
}
