import { useState, useEffect, useCallback, useRef } from 'react'
import { authorizedFetch } from '../../../api/authorizedFetch'
import { useAppSelector } from '../../../hooks'
import type {
  DashboardData,
  UseDashboardDataReturn,
  DashboardAlert,
  DashboardTask,
  DashboardOrder,
  DashboardActivity
} from './types'

const CACHE_KEY = 'caskr_mobile_dashboard_cache'
const CACHE_MAX_AGE = 5 * 60 * 1000 // 5 minutes
const REFRESH_INTERVAL = 60 * 1000 // 60 seconds

interface CachedData {
  data: DashboardData
  timestamp: number
}

/**
 * Get time-of-day greeting
 */
function getGreeting(): string {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 17) return 'Good afternoon'
  return 'Good evening'
}

/**
 * Format current date for display
 */
function formatCurrentDate(): string {
  return new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric'
  })
}

/**
 * Load cached dashboard data from localStorage
 */
function loadCachedData(): CachedData | null {
  try {
    const cached = localStorage.getItem(CACHE_KEY)
    if (!cached) return null
    const parsed = JSON.parse(cached) as CachedData
    return parsed
  } catch {
    return null
  }
}

/**
 * Save dashboard data to localStorage cache
 */
function saveCachedData(data: DashboardData): void {
  try {
    const cacheEntry: CachedData = {
      data,
      timestamp: Date.now()
    }
    localStorage.setItem(CACHE_KEY, JSON.stringify(cacheEntry))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Check if cached data is still valid
 */
function isCacheValid(cached: CachedData | null): boolean {
  if (!cached) return false
  return Date.now() - cached.timestamp < CACHE_MAX_AGE
}

interface FetchResourceOptions<T> {
  fallback: T
  allow404?: boolean
  transform?: (payload: unknown) => T
}

interface FetchResourceResult<T> {
  data: T
  error?: string
}

/**
 * Format HTTP error status to user-friendly message
 */
function formatFetchError(status: number): string {
  if (status >= 500) {
    return 'Our servers are temporarily unavailable. Please try again in a few moments.'
  }
  if (status === 401 || status === 403) {
    return 'Your session has expired. Please sign in again.'
  }
  if (status === 404) {
    return 'The requested data could not be found.'
  }
  if (status === 429) {
    return 'Too many requests. Please wait a moment before trying again.'
  }
  return 'Unable to load data. Please check your connection and try again.'
}

async function fetchResource<T>(
  url: string,
  { fallback, allow404 = false, transform }: FetchResourceOptions<T>
): Promise<FetchResourceResult<T>> {
  try {
    const response = await authorizedFetch(url)

    if (!response.ok) {
      if (allow404 && response.status === 404) {
        return { data: fallback }
      }

      return {
        data: fallback,
        error: formatFetchError(response.status)
      }
    }

    const payload = await response.json()
    const data = transform ? transform(payload) : (payload as T)
    return { data }
  } catch (error) {
    console.error(`[useDashboardData] Error fetching ${url}`, error)
    return {
      data: fallback,
      error: error instanceof Error ? error.message : 'Unknown error'
    }
  }
}

/**
 * Fetch tasks due today from the API
 */
async function fetchTodaysTasks(): Promise<FetchResourceResult<DashboardTask[]>> {
  const today = new Date().toISOString().split('T')[0]
  return fetchResource<DashboardTask[]>(`api/tasks?dueDate=${today}`, { fallback: [] })
}

/**
 * Order API response shape
 */
interface OrderApiResponse {
  id: number
  name: string
  statusId: number
  quantity: number
  filledBarrelCount?: number
  completedBarrelCount?: number
  dueDate?: string
  progress?: number
}

/**
 * Calculate order progress from API data
 * Prefers server-provided progress, falls back to ratio calculation
 */
function calculateOrderProgress(order: OrderApiResponse): number {
  // Use server-provided progress if available
  if (typeof order.progress === 'number') {
    return Math.min(100, Math.max(0, order.progress))
  }

  // Calculate from completed/filled barrel counts
  const completed = order.completedBarrelCount ?? order.filledBarrelCount ?? 0
  const total = order.quantity || 1

  return Math.min(100, Math.round((completed / total) * 100))
}

/**
 * Get barrel count from order data
 * Uses filled count if available, otherwise completed count
 */
function getBarrelCount(order: OrderApiResponse): number {
  return order.filledBarrelCount ?? order.completedBarrelCount ?? 0
}

/**
 * Fetch active orders from the API
 */
async function fetchActiveOrders(): Promise<FetchResourceResult<DashboardOrder[]>> {
  return fetchResource<DashboardOrder[]>(
    'api/orders?status=active&limit=10',
    {
      fallback: [],
      transform: orders =>
        (orders as OrderApiResponse[]).map(order => ({
          id: order.id,
          name: order.name,
          progress: calculateOrderProgress(order),
          barrelCount: getBarrelCount(order),
          totalBarrels: order.quantity,
          status: order.statusId === 1 ? 'In Progress' : 'Pending',
          dueDate: order.dueDate
        }))
    }
  )
}

/**
 * Fetch alerts from the API
 */
async function fetchAlerts(): Promise<FetchResourceResult<DashboardAlert[]>> {
  return fetchResource<DashboardAlert[]>('api/dashboard/alerts', {
    fallback: [],
    allow404: true
  })
}

/**
 * Fetch recent activity from the API
 */
async function fetchRecentActivity(): Promise<FetchResourceResult<DashboardActivity[]>> {
  return fetchResource<DashboardActivity[]>('api/dashboard/activity?limit=5', {
    fallback: [],
    allow404: true
  })
}

/**
 * Fetch dashboard stats
 */
async function fetchStats(): Promise<FetchResourceResult<DashboardData['stats']>> {
  return fetchResource<DashboardData['stats']>('api/dashboard/stats', {
    fallback: { tasksCompletedToday: 0, tasksDueToday: 0, activeBarrels: 0, pendingMovements: 0 }
  })
}

interface DashboardFetchResult {
  data: DashboardData
  errors: string[]
}

/**
 * Hook for fetching and managing dashboard data with SWR-like caching
 * - Fetches tasks, orders, alerts in parallel
 * - Caches data with stale-while-revalidate pattern
 * - Background refresh every 60 seconds when visible
 * - Shows cached data immediately, refreshes in background
 * - Handles offline gracefully with cached data + offline indicator
 */
export function useDashboardData(): UseDashboardDataReturn {
  const user = useAppSelector(state => state.auth.user)
  const [data, setData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isOffline, setIsOffline] = useState(!navigator.onLine)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [dismissedAlerts, setDismissedAlerts] = useState<Set<string>>(new Set())

  const refreshIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const isMountedRef = useRef(true)

  // Handle online/offline status
  useEffect(() => {
    const handleOnline = () => setIsOffline(false)
    const handleOffline = () => setIsOffline(true)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  /**
   * Fetch all dashboard data in parallel
   */
  const fetchDashboardData = useCallback(async (_isBackgroundRefresh = false): Promise<DashboardFetchResult> => {
    // Fetch all data in parallel
    const [tasksResult, ordersResult, alertsResult, activityResult, statsResult] = await Promise.all([
      fetchTodaysTasks(),
      fetchActiveOrders(),
      fetchAlerts(),
      fetchRecentActivity(),
      fetchStats()
    ])

    const errors = [
      tasksResult.error && 'Tasks unavailable',
      ordersResult.error && 'Orders unavailable',
      alertsResult.error && 'Alerts unavailable',
      activityResult.error && 'Activity unavailable',
      statsResult.error && 'Stats unavailable'
    ].filter((message): message is string => Boolean(message))

    const dashboardData: DashboardData = {
      greeting: getGreeting(),
      userName: user?.name || 'User',
      currentDate: formatCurrentDate(),
      alerts: alertsResult.data.filter(alert => !dismissedAlerts.has(alert.id)),
      todaysTasks: tasksResult.data.slice(0, 5), // Max 5 tasks shown
      activeOrders: ordersResult.data.slice(0, 10), // Max 10 orders
      recentActivity: activityResult.data.slice(0, 5), // Max 5 activities
      stats: statsResult.data
    }

    // Cache the data
    saveCachedData(dashboardData)

    return { data: dashboardData, errors }
  }, [user?.name, dismissedAlerts])

  /**
   * Main refresh function
   */
  const refresh = useCallback(async () => {
    if (!isMountedRef.current) return

    setIsRefreshing(true)
    setError(null)

    try {
      const freshData = await fetchDashboardData(true)
      if (freshData && isMountedRef.current) {
        setData(freshData.data)
        setLastUpdated(new Date())
        setError(freshData.errors.length ? 'Some dashboard data could not be updated. Try again shortly.' : null)
      }
    } catch (err) {
      if (isMountedRef.current) {
        setError(err instanceof Error ? err.message : 'Failed to refresh dashboard')
      }
    } finally {
      if (isMountedRef.current) {
        setIsRefreshing(false)
      }
    }
  }, [fetchDashboardData])

  /**
   * Initial data load with stale-while-revalidate
   */
  useEffect(() => {
    isMountedRef.current = true

    const loadData = async () => {
      // 1. Try to show cached data immediately
      const cached = loadCachedData()
      if (cached) {
        // Filter out dismissed alerts from cached data
        setData({
          ...cached.data,
          greeting: getGreeting(),
          currentDate: formatCurrentDate(),
          userName: user?.name || cached.data.userName,
          alerts: cached.data.alerts.filter(a => !dismissedAlerts.has(a.id))
        })
        setLastUpdated(new Date(cached.timestamp))

        // If cache is still valid and we're loading, stop loading state
        if (isCacheValid(cached)) {
          setIsLoading(false)
        }
      }

      // 2. Fetch fresh data in background
      if (!isOffline) {
        try {
          const freshData = await fetchDashboardData()
          if (freshData && isMountedRef.current) {
            setData(freshData.data)
            setLastUpdated(new Date())
            setError(freshData.errors.length ? 'Some dashboard data may be outdated. Pull to refresh to retry.' : null)
          }
        } catch (err) {
          // Only show error if we don't have cached data
          if (!cached && isMountedRef.current) {
            setError(err instanceof Error ? err.message : 'Failed to load dashboard')
          }
        }
      } else if (!cached) {
        setError('You are offline. No cached data available.')
      } else {
        setError('You are offline. Showing cached data. Pull to refresh when back online.')
      }

      if (isMountedRef.current) {
        setIsLoading(false)
      }
    }

    loadData()

    return () => {
      isMountedRef.current = false
    }
  }, [fetchDashboardData, user?.name, isOffline, dismissedAlerts])

  /**
   * Background refresh when page is visible
   */
  useEffect(() => {
    if (isOffline) {
      // Clear interval when offline
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current)
        refreshIntervalRef.current = null
      }
      return
    }

    const handleVisibilityChange = () => {
      if (document.hidden) {
        // Pause refresh when page is hidden
        if (refreshIntervalRef.current) {
          clearInterval(refreshIntervalRef.current)
          refreshIntervalRef.current = null
        }
      } else {
        // Resume refresh when page becomes visible
        refresh()
        refreshIntervalRef.current = setInterval(refresh, REFRESH_INTERVAL)
      }
    }

    // Start refresh interval if page is visible
    if (!document.hidden) {
      refreshIntervalRef.current = setInterval(refresh, REFRESH_INTERVAL)
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current)
      }
    }
  }, [refresh, isOffline])

  /**
   * Dismiss an alert (temporarily until next refresh)
   */
  const dismissAlert = useCallback((alertId: string) => {
    setDismissedAlerts(prev => new Set([...prev, alertId]))
    setData(prev => prev ? {
      ...prev,
      alerts: prev.alerts.filter(a => a.id !== alertId)
    } : null)
  }, [])

  /**
   * Complete a task
   */
  const completeTask = useCallback(async (taskId: number) => {
    // Optimistic update
    setData(prev => {
      if (!prev) return null
      return {
        ...prev,
        todaysTasks: prev.todaysTasks.map(t =>
          t.id === taskId ? { ...t, isComplete: true } : t
        ),
        stats: {
          ...prev.stats,
          tasksCompletedToday: prev.stats.tasksCompletedToday + 1
        }
      }
    })

    try {
      const response = await authorizedFetch(`api/tasks/${taskId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isComplete: true })
      })

      if (!response.ok) {
        throw new Error('Failed to complete task')
      }
    } catch (err) {
      // Rollback on failure
      setData(prev => {
        if (!prev) return null
        return {
          ...prev,
          todaysTasks: prev.todaysTasks.map(t =>
            t.id === taskId ? { ...t, isComplete: false } : t
          ),
          stats: {
            ...prev.stats,
            tasksCompletedToday: prev.stats.tasksCompletedToday - 1
          }
        }
      })
      throw err
    }
  }, [])

  /**
   * Undo task completion
   */
  const undoCompleteTask = useCallback(async (taskId: number) => {
    // Optimistic update
    setData(prev => {
      if (!prev) return null
      return {
        ...prev,
        todaysTasks: prev.todaysTasks.map(t =>
          t.id === taskId ? { ...t, isComplete: false } : t
        ),
        stats: {
          ...prev.stats,
          tasksCompletedToday: Math.max(0, prev.stats.tasksCompletedToday - 1)
        }
      }
    })

    try {
      const response = await authorizedFetch(`api/tasks/${taskId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isComplete: false })
      })

      if (!response.ok) {
        throw new Error('Failed to undo task completion')
      }
    } catch (err) {
      // Rollback on failure
      setData(prev => {
        if (!prev) return null
        return {
          ...prev,
          todaysTasks: prev.todaysTasks.map(t =>
            t.id === taskId ? { ...t, isComplete: true } : t
          ),
          stats: {
            ...prev.stats,
            tasksCompletedToday: prev.stats.tasksCompletedToday + 1
          }
        }
      })
      throw err
    }
  }, [])

  return {
    data,
    isLoading,
    isRefreshing,
    error,
    isOffline,
    lastUpdated,
    refresh,
    dismissAlert,
    completeTask,
    undoCompleteTask
  }
}

export default useDashboardData
