/**
 * Offline Storage Layer
 *
 * IndexedDB wrapper for offline data storage with type safety.
 */

const DB_NAME = 'caskr_offline'
const DB_VERSION = 1

// Store names
export const STORES = {
  API_CACHE: 'api_cache',
  SYNC_QUEUE: 'sync_queue',
  BARRELS: 'barrels',
  TASKS: 'tasks',
  ORDERS: 'orders',
} as const

type StoreName = (typeof STORES)[keyof typeof STORES]

interface CachedResponse {
  url: string
  data: unknown
  timestamp: number
  etag?: string
}

interface SyncQueueItem {
  id: string
  url: string
  method: string
  body?: string
  headers: Record<string, string>
  timestamp: number
  retries: number
  maxRetries: number
}

let dbPromise: Promise<IDBDatabase> | null = null

/**
 * Open or create the IndexedDB database
 */
function openDatabase(): Promise<IDBDatabase> {
  if (dbPromise) return dbPromise

  dbPromise = new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION)

    request.onerror = () => {
      console.error('[OfflineStorage] Failed to open database:', request.error)
      reject(request.error)
    }

    request.onsuccess = () => {
      resolve(request.result)
    }

    request.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result

      // API cache store
      if (!db.objectStoreNames.contains(STORES.API_CACHE)) {
        const cacheStore = db.createObjectStore(STORES.API_CACHE, { keyPath: 'url' })
        cacheStore.createIndex('timestamp', 'timestamp', { unique: false })
      }

      // Sync queue store
      if (!db.objectStoreNames.contains(STORES.SYNC_QUEUE)) {
        const syncStore = db.createObjectStore(STORES.SYNC_QUEUE, { keyPath: 'id' })
        syncStore.createIndex('timestamp', 'timestamp', { unique: false })
      }

      // Barrels store
      if (!db.objectStoreNames.contains(STORES.BARRELS)) {
        const barrelsStore = db.createObjectStore(STORES.BARRELS, { keyPath: 'id' })
        barrelsStore.createIndex('sku', 'sku', { unique: true })
        barrelsStore.createIndex('timestamp', 'timestamp', { unique: false })
      }

      // Tasks store
      if (!db.objectStoreNames.contains(STORES.TASKS)) {
        const tasksStore = db.createObjectStore(STORES.TASKS, { keyPath: 'id' })
        tasksStore.createIndex('dueDate', 'dueDate', { unique: false })
        tasksStore.createIndex('timestamp', 'timestamp', { unique: false })
      }

      // Orders store
      if (!db.objectStoreNames.contains(STORES.ORDERS)) {
        const ordersStore = db.createObjectStore(STORES.ORDERS, { keyPath: 'id' })
        ordersStore.createIndex('timestamp', 'timestamp', { unique: false })
      }
    }
  })

  return dbPromise
}

/**
 * Generic get operation
 */
async function get<T>(storeName: StoreName, key: IDBValidKey): Promise<T | undefined> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readonly')
    const store = transaction.objectStore(storeName)
    const request = store.get(key)

    request.onsuccess = () => resolve(request.result as T | undefined)
    request.onerror = () => reject(request.error)
  })
}

/**
 * Generic put operation
 */
async function put<T>(storeName: StoreName, data: T): Promise<void> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readwrite')
    const store = transaction.objectStore(storeName)
    const request = store.put(data)

    request.onsuccess = () => resolve()
    request.onerror = () => reject(request.error)
  })
}

/**
 * Generic delete operation
 */
async function remove(storeName: StoreName, key: IDBValidKey): Promise<void> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readwrite')
    const store = transaction.objectStore(storeName)
    const request = store.delete(key)

    request.onsuccess = () => resolve()
    request.onerror = () => reject(request.error)
  })
}

/**
 * Get all items from a store
 */
async function getAll<T>(storeName: StoreName): Promise<T[]> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readonly')
    const store = transaction.objectStore(storeName)
    const request = store.getAll()

    request.onsuccess = () => resolve(request.result as T[])
    request.onerror = () => reject(request.error)
  })
}

/**
 * Clear all items from a store
 */
async function clear(storeName: StoreName): Promise<void> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readwrite')
    const store = transaction.objectStore(storeName)
    const request = store.clear()

    request.onsuccess = () => resolve()
    request.onerror = () => reject(request.error)
  })
}

/**
 * Delete items older than specified time
 */
async function deleteOlderThan(storeName: StoreName, maxAge: number): Promise<void> {
  const db = await openDatabase()
  const cutoff = Date.now() - maxAge

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readwrite')
    const store = transaction.objectStore(storeName)
    const index = store.index('timestamp')
    const range = IDBKeyRange.upperBound(cutoff)
    const request = index.openCursor(range)

    request.onsuccess = () => {
      const cursor = request.result
      if (cursor) {
        cursor.delete()
        cursor.continue()
      } else {
        resolve()
      }
    }
    request.onerror = () => reject(request.error)
  })
}

// ============================================
// API Cache Operations
// ============================================

const MAX_CACHE_AGE = 24 * 60 * 60 * 1000 // 24 hours
const MAX_CACHE_SIZE = 100

/**
 * Cache an API response
 */
export async function cacheApiResponse(
  url: string,
  data: unknown,
  etag?: string
): Promise<void> {
  const cached: CachedResponse = {
    url,
    data,
    timestamp: Date.now(),
    etag,
  }

  await put(STORES.API_CACHE, cached)

  // Cleanup old entries
  await deleteOlderThan(STORES.API_CACHE, MAX_CACHE_AGE)

  // Limit cache size
  const allCached = await getAll<CachedResponse>(STORES.API_CACHE)
  if (allCached.length > MAX_CACHE_SIZE) {
    const sorted = allCached.sort((a, b) => a.timestamp - b.timestamp)
    const toDelete = sorted.slice(0, allCached.length - MAX_CACHE_SIZE)
    for (const item of toDelete) {
      await remove(STORES.API_CACHE, item.url)
    }
  }
}

/**
 * Get cached API response
 */
export async function getCachedResponse(url: string): Promise<CachedResponse | undefined> {
  const cached = await get<CachedResponse>(STORES.API_CACHE, url)

  if (cached) {
    const age = Date.now() - cached.timestamp
    if (age > MAX_CACHE_AGE) {
      await remove(STORES.API_CACHE, url)
      return undefined
    }
  }

  return cached
}

/**
 * Invalidate cached response
 */
export async function invalidateCache(url: string): Promise<void> {
  await remove(STORES.API_CACHE, url)
}

/**
 * Clear all API cache
 */
export async function clearApiCache(): Promise<void> {
  await clear(STORES.API_CACHE)
}

// ============================================
// Sync Queue Operations
// ============================================

/**
 * Add an item to the sync queue
 */
export async function addToSyncQueue(
  url: string,
  method: string,
  body?: string,
  headers: Record<string, string> = {}
): Promise<string> {
  const id = `sync_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`

  const item: SyncQueueItem = {
    id,
    url,
    method,
    body,
    headers,
    timestamp: Date.now(),
    retries: 0,
    maxRetries: 3,
  }

  await put(STORES.SYNC_QUEUE, item)
  return id
}

/**
 * Get all pending sync items
 */
export async function getSyncQueue(): Promise<SyncQueueItem[]> {
  return getAll<SyncQueueItem>(STORES.SYNC_QUEUE)
}

/**
 * Update sync item retry count
 */
export async function updateSyncItemRetries(id: string, retries: number): Promise<void> {
  const item = await get<SyncQueueItem>(STORES.SYNC_QUEUE, id)
  if (item) {
    item.retries = retries
    await put(STORES.SYNC_QUEUE, item)
  }
}

/**
 * Remove item from sync queue
 */
export async function removeSyncItem(id: string): Promise<void> {
  await remove(STORES.SYNC_QUEUE, id)
}

/**
 * Get sync queue count
 */
export async function getSyncQueueCount(): Promise<number> {
  const items = await getAll<SyncQueueItem>(STORES.SYNC_QUEUE)
  return items.length
}

// ============================================
// Entity Storage Operations
// ============================================

/**
 * Cache entities with timestamp
 */
export async function cacheEntities<T extends { id: number }>(
  storeName: StoreName,
  entities: T[]
): Promise<void> {
  const db = await openDatabase()

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, 'readwrite')
    const store = transaction.objectStore(storeName)

    for (const entity of entities) {
      store.put({ ...entity, timestamp: Date.now() })
    }

    transaction.oncomplete = () => resolve()
    transaction.onerror = () => reject(transaction.error)
  })
}

/**
 * Get all cached entities
 */
export async function getCachedEntities<T>(storeName: StoreName): Promise<T[]> {
  return getAll<T>(storeName)
}

/**
 * Get single cached entity
 */
export async function getCachedEntity<T>(
  storeName: StoreName,
  id: number
): Promise<T | undefined> {
  return get<T>(storeName, id)
}

/**
 * Clear entity cache
 */
export async function clearEntityCache(storeName: StoreName): Promise<void> {
  await clear(storeName)
}

// ============================================
// Utility Operations
// ============================================

/**
 * Check if IndexedDB is available
 */
export function isIndexedDBAvailable(): boolean {
  return 'indexedDB' in window
}

/**
 * Get database storage usage estimate
 */
export async function getStorageEstimate(): Promise<{
  usage?: number
  quota?: number
}> {
  if ('storage' in navigator && 'estimate' in navigator.storage) {
    return navigator.storage.estimate()
  }
  return {}
}

/**
 * Close database connection
 */
export async function closeDatabase(): Promise<void> {
  if (dbPromise) {
    const db = await dbPromise
    db.close()
    dbPromise = null
  }
}

export default {
  cacheApiResponse,
  getCachedResponse,
  invalidateCache,
  clearApiCache,
  addToSyncQueue,
  getSyncQueue,
  updateSyncItemRetries,
  removeSyncItem,
  getSyncQueueCount,
  cacheEntities,
  getCachedEntities,
  getCachedEntity,
  clearEntityCache,
  isIndexedDBAvailable,
  getStorageEstimate,
  closeDatabase,
  STORES,
}
