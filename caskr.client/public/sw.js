/**
 * Caskr Service Worker
 *
 * Implements caching strategies for offline support.
 */

const CACHE_VERSION = 'v1'
const CACHE_NAME = `caskr-${CACHE_VERSION}`
const API_CACHE_NAME = `caskr-api-${CACHE_VERSION}`

// App shell files to cache (Cache First strategy)
const APP_SHELL_FILES = [
  '/',
  '/index.html',
  '/manifest.json',
]

// API routes that should use Network First strategy
const API_PATTERNS = [
  /^\/api\/.*/,
]

// Static assets that should use Cache First strategy
const STATIC_PATTERNS = [
  /\.(?:js|css|woff2?|ttf|eot|ico|png|jpg|jpeg|svg|gif|webp)$/,
]

/**
 * Install event - cache app shell
 */
self.addEventListener('install', (event) => {
  console.log('[ServiceWorker] Install')

  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('[ServiceWorker] Caching app shell')
        return cache.addAll(APP_SHELL_FILES)
      })
      .then(() => {
        // Skip waiting to activate immediately
        return self.skipWaiting()
      })
      .catch((err) => {
        console.error('[ServiceWorker] Failed to cache app shell:', err)
      })
  )
})

/**
 * Activate event - clean up old caches
 */
self.addEventListener('activate', (event) => {
  console.log('[ServiceWorker] Activate')

  event.waitUntil(
    caches.keys()
      .then((cacheNames) => {
        return Promise.all(
          cacheNames
            .filter((name) => name.startsWith('caskr-') && name !== CACHE_NAME && name !== API_CACHE_NAME)
            .map((name) => {
              console.log('[ServiceWorker] Deleting old cache:', name)
              return caches.delete(name)
            })
        )
      })
      .then(() => {
        // Take control of all clients immediately
        return self.clients.claim()
      })
  )
})

/**
 * Fetch event - handle requests with appropriate strategy
 */
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url)

  // Skip non-GET requests for caching (but let them through)
  if (event.request.method !== 'GET') {
    return
  }

  // Skip cross-origin requests
  if (url.origin !== self.location.origin) {
    return
  }

  // API requests: Network First with Cache Fallback
  if (API_PATTERNS.some((pattern) => pattern.test(url.pathname))) {
    event.respondWith(networkFirstWithCache(event.request, API_CACHE_NAME))
    return
  }

  // Static assets: Cache First
  if (STATIC_PATTERNS.some((pattern) => pattern.test(url.pathname))) {
    event.respondWith(cacheFirstWithNetwork(event.request, CACHE_NAME))
    return
  }

  // Navigation requests: Network First with App Shell Fallback
  if (event.request.mode === 'navigate') {
    event.respondWith(networkFirstWithAppShell(event.request))
    return
  }

  // Default: Network with Cache Fallback
  event.respondWith(networkFirstWithCache(event.request, CACHE_NAME))
})

/**
 * Network First with Cache Fallback
 * Best for: API calls, dynamic content
 */
async function networkFirstWithCache(request, cacheName) {
  try {
    const networkResponse = await fetch(request)

    // Cache successful responses
    if (networkResponse.ok) {
      const cache = await caches.open(cacheName)
      cache.put(request, networkResponse.clone())
    }

    return networkResponse
  } catch (error) {
    console.log('[ServiceWorker] Network failed, trying cache:', request.url)

    const cachedResponse = await caches.match(request)
    if (cachedResponse) {
      return cachedResponse
    }

    // Return error response for API calls
    return new Response(
      JSON.stringify({ error: 'Network unavailable', offline: true }),
      {
        status: 503,
        headers: { 'Content-Type': 'application/json' },
      }
    )
  }
}

/**
 * Cache First with Network Fallback
 * Best for: Static assets, fonts, images
 */
async function cacheFirstWithNetwork(request, cacheName) {
  const cachedResponse = await caches.match(request)

  if (cachedResponse) {
    // Update cache in background
    updateCacheInBackground(request, cacheName)
    return cachedResponse
  }

  try {
    const networkResponse = await fetch(request)

    if (networkResponse.ok) {
      const cache = await caches.open(cacheName)
      cache.put(request, networkResponse.clone())
    }

    return networkResponse
  } catch (error) {
    console.error('[ServiceWorker] Cache and network failed:', request.url)
    throw error
  }
}

/**
 * Network First with App Shell Fallback
 * Best for: Navigation requests
 */
async function networkFirstWithAppShell(request) {
  try {
    const networkResponse = await fetch(request)
    return networkResponse
  } catch (error) {
    console.log('[ServiceWorker] Navigation failed, serving app shell')

    // Return cached index.html for SPA navigation
    const cachedResponse = await caches.match('/index.html')
    if (cachedResponse) {
      return cachedResponse
    }

    // Last resort - offline page
    return new Response(
      `<!DOCTYPE html>
      <html>
        <head>
          <title>Caskr - Offline</title>
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            body {
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
              display: flex;
              align-items: center;
              justify-content: center;
              min-height: 100vh;
              margin: 0;
              background: #f3f4f6;
              color: #374151;
              text-align: center;
              padding: 20px;
            }
            h1 { font-size: 24px; margin-bottom: 8px; }
            p { color: #6b7280; margin-bottom: 20px; }
            button {
              padding: 12px 24px;
              background: #2563eb;
              color: white;
              border: none;
              border-radius: 8px;
              font-size: 16px;
              cursor: pointer;
            }
          </style>
        </head>
        <body>
          <div>
            <h1>You're offline</h1>
            <p>Please check your internet connection and try again.</p>
            <button onclick="location.reload()">Retry</button>
          </div>
        </body>
      </html>`,
      { headers: { 'Content-Type': 'text/html' } }
    )
  }
}

/**
 * Update cache in background (stale-while-revalidate pattern)
 */
function updateCacheInBackground(request, cacheName) {
  fetch(request)
    .then((response) => {
      if (response.ok) {
        caches.open(cacheName).then((cache) => {
          cache.put(request, response)
        })
      }
    })
    .catch(() => {
      // Ignore background update failures
    })
}

/**
 * Background sync handler
 */
self.addEventListener('sync', (event) => {
  console.log('[ServiceWorker] Sync event:', event.tag)

  if (event.tag === 'caskr-sync') {
    event.waitUntil(processPendingRequests())
  }
})

/**
 * Process pending requests from IndexedDB
 */
async function processPendingRequests() {
  // This will be handled by the main app's background sync service
  // Service worker just triggers the sync event
  const clients = await self.clients.matchAll()
  for (const client of clients) {
    client.postMessage({ type: 'SYNC_REQUESTED' })
  }
}

/**
 * Handle messages from main thread
 */
self.addEventListener('message', (event) => {
  console.log('[ServiceWorker] Message:', event.data)

  if (event.data.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }

  if (event.data.type === 'CLEAR_CACHE') {
    caches.keys().then((names) => {
      names.forEach((name) => caches.delete(name))
    })
  }
})

/**
 * Push notification handler (for future use)
 */
self.addEventListener('push', (event) => {
  if (!event.data) return

  const data = event.data.json()

  event.waitUntil(
    self.registration.showNotification(data.title || 'Caskr', {
      body: data.body,
      icon: '/icons/icon-192x192.png',
      badge: '/icons/icon-72x72.png',
      data: data.data,
    })
  )
})

/**
 * Notification click handler
 */
self.addEventListener('notificationclick', (event) => {
  event.notification.close()

  const urlToOpen = event.notification.data?.url || '/'

  event.waitUntil(
    self.clients.matchAll({ type: 'window' }).then((clients) => {
      // Focus existing window if available
      for (const client of clients) {
        if (client.url === urlToOpen && 'focus' in client) {
          return client.focus()
        }
      }

      // Open new window
      if (self.clients.openWindow) {
        return self.clients.openWindow(urlToOpen)
      }
    })
  )
})

console.log('[ServiceWorker] Loaded')
