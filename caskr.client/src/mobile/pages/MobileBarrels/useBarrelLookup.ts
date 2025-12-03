import { useState, useCallback, useEffect, useRef } from 'react'
import { authorizedFetch } from '../../../api/authorizedFetch'
import type {
  BarrelDetail,
  BarrelSearchResult,
  BarrelHistoryItem,
  BarrelStatus,
  ScanResult,
  GaugeRecordData,
  PendingAction,
  UseBarrelLookupReturn,
  Rickhouse
} from './types'

const CACHE_KEY = 'caskr_barrel_cache'
const RECENT_BARRELS_KEY = 'caskr_recent_barrels'
const PENDING_ACTIONS_KEY = 'caskr_pending_barrel_actions'
const MAX_CACHED_BARRELS = 50
const MAX_RECENT_BARRELS = 5
const SEARCH_DEBOUNCE_MS = 300

interface CachedBarrel {
  detail: BarrelDetail
  history: BarrelHistoryItem[]
  timestamp: number
}

/**
 * Load cached barrels from IndexedDB
 */
function loadCachedBarrels(): Map<number, CachedBarrel> {
  try {
    const cached = localStorage.getItem(CACHE_KEY)
    if (!cached) return new Map()
    const entries = JSON.parse(cached) as Array<[number, CachedBarrel]>
    return new Map(entries)
  } catch {
    return new Map()
  }
}

/**
 * Save barrel to cache with LRU eviction
 */
function cacheBarrel(barrelId: number, detail: BarrelDetail, history: BarrelHistoryItem[]): void {
  try {
    const cache = loadCachedBarrels()

    // LRU eviction if at capacity
    if (cache.size >= MAX_CACHED_BARRELS && !cache.has(barrelId)) {
      const oldest = [...cache.entries()].sort((a, b) => a[1].timestamp - b[1].timestamp)[0]
      if (oldest) {
        cache.delete(oldest[0])
      }
    }

    cache.set(barrelId, { detail, history, timestamp: Date.now() })
    localStorage.setItem(CACHE_KEY, JSON.stringify([...cache.entries()]))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Load recent barrels from localStorage
 */
function loadRecentBarrels(): BarrelSearchResult[] {
  try {
    const cached = localStorage.getItem(RECENT_BARRELS_KEY)
    if (!cached) return []
    return JSON.parse(cached) as BarrelSearchResult[]
  } catch {
    return []
  }
}

/**
 * Add barrel to recent list
 */
function addToRecentBarrels(barrel: BarrelSearchResult): void {
  try {
    const recent = loadRecentBarrels().filter(b => b.id !== barrel.id)
    recent.unshift(barrel)
    const trimmed = recent.slice(0, MAX_RECENT_BARRELS)
    localStorage.setItem(RECENT_BARRELS_KEY, JSON.stringify(trimmed))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Load pending actions for offline sync
 */
function loadPendingActions(): PendingAction[] {
  try {
    const cached = localStorage.getItem(PENDING_ACTIONS_KEY)
    if (!cached) return []
    return JSON.parse(cached) as PendingAction[]
  } catch {
    return []
  }
}

/**
 * Save pending action for offline sync
 */
function savePendingAction(action: PendingAction): void {
  try {
    const pending = loadPendingActions()
    pending.push(action)
    localStorage.setItem(PENDING_ACTIONS_KEY, JSON.stringify(pending))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Remove completed pending action
 */
function removePendingAction(actionId: string): void {
  try {
    const pending = loadPendingActions().filter(a => a.id !== actionId)
    localStorage.setItem(PENDING_ACTIONS_KEY, JSON.stringify(pending))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Hook for managing barrel lookup, search, and actions
 */
export function useBarrelLookup(): UseBarrelLookupReturn {
  // Scan state
  const [scanResult, setScanResult] = useState<ScanResult | null>(null)
  const [isScannerActive, setIsScannerActive] = useState(true)

  // Search state
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<BarrelSearchResult[]>([])
  const [isSearching, setIsSearching] = useState(false)
  const [searchError, setSearchError] = useState<string | null>(null)

  // Filter state
  const [filterStatus, setFilterStatus] = useState<BarrelStatus | 'all'>('all')
  const [filterRickhouse, setFilterRickhouse] = useState<number | null>(null)

  // Barrel detail state
  const [selectedBarrel, setSelectedBarrel] = useState<BarrelDetail | null>(null)
  const [barrelHistory, setBarrelHistory] = useState<BarrelHistoryItem[]>([])
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState<string | null>(null)

  // Recent barrels
  const [recentBarrels, setRecentBarrels] = useState<BarrelSearchResult[]>([])

  // Offline state
  const [isOffline, setIsOffline] = useState(!navigator.onLine)
  const [pendingActions, setPendingActions] = useState<PendingAction[]>([])

  // Refs
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Load initial data
  useEffect(() => {
    setRecentBarrels(loadRecentBarrels())
    setPendingActions(loadPendingActions())
  }, [])

  // Handle online/offline status
  useEffect(() => {
    const handleOnline = () => {
      setIsOffline(false)
      syncPendingActions()
    }
    const handleOffline = () => setIsOffline(true)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  // Sync pending actions when online
  const syncPendingActions = useCallback(async () => {
    const pending = loadPendingActions()
    if (pending.length === 0) return

    for (const action of pending) {
      try {
        if (action.type === 'movement') {
          await authorizedFetch(`api/barrels/${action.barrelId}/movements`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(action.data)
          })
        } else if (action.type === 'gauge') {
          await authorizedFetch(`api/barrels/${action.barrelId}/gauges`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(action.data)
          })
        }
        removePendingAction(action.id)
        setPendingActions(prev => prev.filter(a => a.id !== action.id))
      } catch (error) {
        console.error('[useBarrelLookup] Failed to sync action:', error)
        // Keep action in queue for retry
      }
    }
  }, [])

  // Toggle scanner
  const toggleScanner = useCallback(() => {
    setIsScannerActive(prev => !prev)
  }, [])

  // Clear scan result
  const clearScanResult = useCallback(() => {
    setScanResult(null)
  }, [])

  // Search barrels
  const performSearch = useCallback(async (query: string, status: BarrelStatus | 'all', rickhouseId: number | null) => {
    if (!query.trim()) {
      setSearchResults([])
      setIsSearching(false)
      return
    }

    setIsSearching(true)
    setSearchError(null)

    try {
      let url = `api/barrels/search?q=${encodeURIComponent(query)}`
      if (status !== 'all') {
        url += `&status=${status}`
      }
      if (rickhouseId !== null) {
        url += `&rickhouseId=${rickhouseId}`
      }

      const response = await authorizedFetch(url)
      if (!response.ok) {
        throw new Error('Search failed')
      }

      const results = await response.json() as BarrelSearchResult[]
      setSearchResults(results)
    } catch (error) {
      console.error('[useBarrelLookup] Search error:', error)
      setSearchError(error instanceof Error ? error.message : 'Search failed')
      setSearchResults([])
    } finally {
      setIsSearching(false)
    }
  }, [])

  // Debounced search
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }

    searchTimeoutRef.current = setTimeout(() => {
      performSearch(searchQuery, filterStatus, filterRickhouse)
    }, SEARCH_DEBOUNCE_MS)

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current)
      }
    }
  }, [searchQuery, filterStatus, filterRickhouse, performSearch])

  // Load barrel detail by ID
  const loadBarrelDetail = useCallback(async (barrelId: number) => {
    setIsLoadingDetail(true)
    setDetailError(null)

    // Check cache first
    const cache = loadCachedBarrels()
    const cached = cache.get(barrelId)
    if (cached) {
      setSelectedBarrel(cached.detail)
      setBarrelHistory(cached.history)
      setIsLoadingDetail(false)

      // Add to recent
      addToRecentBarrels({
        id: cached.detail.id,
        sku: cached.detail.sku,
        status: cached.detail.status,
        rickhouseName: cached.detail.rickhouseName,
        age: cached.detail.age,
        batchName: cached.detail.batchName,
        spiritType: cached.detail.spiritType
      })
      setRecentBarrels(loadRecentBarrels())

      // Refresh in background if online
      if (!isOffline) {
        fetchBarrelDetail(barrelId, true)
      }
      return
    }

    // Fetch from API
    await fetchBarrelDetail(barrelId, false)
  }, [isOffline])

  // Fetch barrel detail from API
  const fetchBarrelDetail = async (barrelId: number, isBackgroundRefresh: boolean) => {
    try {
      const [detailResponse, historyResponse] = await Promise.all([
        authorizedFetch(`api/barrels/${barrelId}`),
        authorizedFetch(`api/barrels/${barrelId}/history?limit=10`)
      ])

      if (!detailResponse.ok) {
        throw new Error('Failed to load barrel')
      }

      const detail = await detailResponse.json() as BarrelDetail
      const history = historyResponse.ok ? await historyResponse.json() as BarrelHistoryItem[] : []

      if (!isBackgroundRefresh) {
        setSelectedBarrel(detail)
        setBarrelHistory(history)
      } else {
        // Only update if still showing same barrel
        setSelectedBarrel(prev => prev?.id === barrelId ? detail : prev)
        setBarrelHistory(prev => prev.length > 0 ? history : prev)
      }

      // Cache the data
      cacheBarrel(barrelId, detail, history)

      // Add to recent
      addToRecentBarrels({
        id: detail.id,
        sku: detail.sku,
        status: detail.status,
        rickhouseName: detail.rickhouseName,
        age: detail.age,
        batchName: detail.batchName,
        spiritType: detail.spiritType
      })
      setRecentBarrels(loadRecentBarrels())
    } catch (error) {
      console.error('[useBarrelLookup] Load detail error:', error)
      if (!selectedBarrel) {
        setDetailError(error instanceof Error ? error.message : 'Failed to load barrel')
      }
    } finally {
      setIsLoadingDetail(false)
    }
  }

  // Load barrel by SKU (from scan)
  const loadBarrelBySku = useCallback(async (sku: string) => {
    setIsLoadingDetail(true)
    setDetailError(null)

    try {
      const response = await authorizedFetch(`api/barrels/sku/${encodeURIComponent(sku)}`)
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error(`Barrel ${sku} not found`)
        }
        throw new Error('Failed to load barrel')
      }

      const detail = await response.json() as BarrelDetail
      setSelectedBarrel(detail)

      // Load history
      const historyResponse = await authorizedFetch(`api/barrels/${detail.id}/history?limit=10`)
      const history = historyResponse.ok ? await historyResponse.json() as BarrelHistoryItem[] : []
      setBarrelHistory(history)

      // Cache
      cacheBarrel(detail.id, detail, history)

      // Add to recent
      addToRecentBarrels({
        id: detail.id,
        sku: detail.sku,
        status: detail.status,
        rickhouseName: detail.rickhouseName,
        age: detail.age,
        batchName: detail.batchName,
        spiritType: detail.spiritType
      })
      setRecentBarrels(loadRecentBarrels())

      // Update scan result
      setScanResult({ type: 'barcode', value: sku, timestamp: new Date() })
    } catch (error) {
      console.error('[useBarrelLookup] Load by SKU error:', error)
      setDetailError(error instanceof Error ? error.message : 'Failed to load barrel')
    } finally {
      setIsLoadingDetail(false)
    }
  }, [])

  // Close detail sheet
  const closeDetail = useCallback(() => {
    setSelectedBarrel(null)
    setBarrelHistory([])
    setDetailError(null)
  }, [])

  // Record movement
  const recordMovement = useCallback(async (barrelId: number, destinationId: number, notes?: string) => {
    const action: PendingAction = {
      id: `movement-${barrelId}-${Date.now()}`,
      type: 'movement',
      barrelId,
      data: { destinationRickhouseId: destinationId, notes },
      timestamp: new Date().toISOString()
    }

    if (isOffline) {
      savePendingAction(action)
      setPendingActions(prev => [...prev, action])

      // Optimistic update
      setSelectedBarrel(prev => prev ? {
        ...prev,
        rickhouseId: destinationId
      } : null)
      return
    }

    try {
      const response = await authorizedFetch(`api/barrels/${barrelId}/movements`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ destinationRickhouseId: destinationId, notes })
      })

      if (!response.ok) {
        throw new Error('Failed to record movement')
      }

      // Reload detail to get updated data
      await loadBarrelDetail(barrelId)
    } catch (error) {
      console.error('[useBarrelLookup] Movement error:', error)
      throw error
    }
  }, [isOffline, loadBarrelDetail])

  // Record gauge
  const recordGauge = useCallback(async (barrelId: number, data: GaugeRecordData) => {
    const action: PendingAction = {
      id: `gauge-${barrelId}-${Date.now()}`,
      type: 'gauge',
      barrelId,
      data,
      timestamp: new Date().toISOString()
    }

    if (isOffline) {
      savePendingAction(action)
      setPendingActions(prev => [...prev, action])

      // Optimistic update
      setSelectedBarrel(prev => prev ? {
        ...prev,
        currentProofGallons: data.proofGallons,
        proof: data.proof,
        lastGaugeDate: new Date().toISOString()
      } : null)
      return
    }

    try {
      const response = await authorizedFetch(`api/barrels/${barrelId}/gauges`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      if (!response.ok) {
        throw new Error('Failed to record gauge')
      }

      // Reload detail to get updated data
      await loadBarrelDetail(barrelId)
    } catch (error) {
      console.error('[useBarrelLookup] Gauge error:', error)
      throw error
    }
  }, [isOffline, loadBarrelDetail])

  return {
    // Scan state
    scanResult,
    isScannerActive,
    toggleScanner,
    clearScanResult,

    // Search state
    searchQuery,
    setSearchQuery,
    searchResults,
    isSearching,
    searchError,

    // Filter state
    filterStatus,
    setFilterStatus,
    filterRickhouse,
    setFilterRickhouse,

    // Barrel detail state
    selectedBarrel,
    barrelHistory,
    isLoadingDetail,
    detailError,
    loadBarrelDetail,
    loadBarrelBySku,
    closeDetail,

    // Recent barrels
    recentBarrels,

    // Actions
    recordMovement,
    recordGauge,

    // Offline state
    isOffline,
    pendingActions
  }
}

// Also export a hook to fetch rickhouses
export function useRickhouses(): { rickhouses: Rickhouse[]; isLoading: boolean } {
  const [rickhouses, setRickhouses] = useState<Rickhouse[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const fetchRickhouses = async () => {
      try {
        const response = await authorizedFetch('api/rickhouses')
        if (response.ok) {
          const data = await response.json() as Rickhouse[]
          setRickhouses(data)
        }
      } catch (error) {
        console.error('[useRickhouses] Error:', error)
      } finally {
        setIsLoading(false)
      }
    }

    fetchRickhouses()
  }, [])

  return { rickhouses, isLoading }
}

export default useBarrelLookup
