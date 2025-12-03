import { useState, useRef, useCallback, useEffect } from 'react'
import { useSafeArea } from '../../hooks'
import { useDashboardData } from './useDashboardData'
import {
  AlertBanner,
  QuickActions,
  TodaysPriorities,
  ActiveOrders,
  RecentActivity
} from './components'
import styles from './MobileDashboard.module.css'

// Pull-to-refresh constants
const PULL_THRESHOLD = 80
const MAX_PULL = 120

// Refresh icon
const RefreshIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.refreshIcon}>
    <path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/>
  </svg>
)

// Offline icon
const OfflineIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.offlineIcon}>
    <path d="M23.64 7c-.45-.34-4.93-4-11.64-4-1.5 0-2.89.19-4.15.48L18.18 13.8 23.64 7zm-6.6 8.22L3.27 1.44 2 2.72l2.05 2.06C1.91 5.76.59 6.82.36 7L12 21.5l3.07-4.04 3.52 3.52 1.26-1.27-2.81-2.81-.18.18v.04-.04l-.02.02.02-.02z"/>
  </svg>
)

/**
 * Mobile dashboard page with pull-to-refresh
 * Layout: Greeting, Alerts, Quick Actions, Today's Priorities, Active Orders, Recent Activity
 */
export function MobileDashboard() {
  const safeArea = useSafeArea()
  const {
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
  } = useDashboardData()

  const containerRef = useRef<HTMLDivElement>(null)
  const [pullDistance, setPullDistance] = useState(0)
  const [isPulling, setIsPulling] = useState(false)
  const touchStartY = useRef<number | null>(null)
  const scrollTop = useRef(0)

  // Track scroll position
  const handleScroll = useCallback((e: Event) => {
    const target = e.target as HTMLElement
    scrollTop.current = target.scrollTop
  }, [])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    container.addEventListener('scroll', handleScroll)
    return () => container.removeEventListener('scroll', handleScroll)
  }, [handleScroll])

  // Touch handlers for pull-to-refresh
  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (scrollTop.current <= 0) {
      touchStartY.current = e.touches[0].clientY
      setIsPulling(true)
    }
  }, [])

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    if (touchStartY.current === null || isRefreshing) return

    const deltaY = e.touches[0].clientY - touchStartY.current

    // Only trigger pull-to-refresh when at top and pulling down
    if (deltaY > 0 && scrollTop.current <= 0) {
      // Apply elastic resistance
      const resistance = 0.5
      const adjustedDelta = Math.min(deltaY * resistance, MAX_PULL)
      setPullDistance(adjustedDelta)

      // Prevent default scroll when pulling
      if (adjustedDelta > 10) {
        e.preventDefault()
      }
    }
  }, [isRefreshing])

  const handleTouchEnd = useCallback(async () => {
    if (!isPulling) return

    touchStartY.current = null
    setIsPulling(false)

    if (pullDistance >= PULL_THRESHOLD && !isRefreshing) {
      // Trigger haptic feedback
      if ('vibrate' in navigator) {
        navigator.vibrate(20)
      }

      // Hold at threshold during refresh
      setPullDistance(PULL_THRESHOLD)

      try {
        await refresh()
      } finally {
        // Animate back to 0
        setPullDistance(0)
      }
    } else {
      // Animate back to 0
      setPullDistance(0)
    }
  }, [isPulling, pullDistance, isRefreshing, refresh])

  // Format last updated time
  const formatLastUpdated = () => {
    if (!lastUpdated) return ''
    const diffMs = Date.now() - lastUpdated.getTime()
    const diffMins = Math.floor(diffMs / (1000 * 60))
    if (diffMins < 1) return 'Updated just now'
    if (diffMins < 60) return `Updated ${diffMins}m ago`
    return `Updated ${lastUpdated.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}`
  }

  // Calculate pull progress for styling
  const pullProgress = Math.min(pullDistance / PULL_THRESHOLD, 1)
  const refreshRotation = pullProgress * 360

  // Loading state
  if (isLoading && !data) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.loadingSpinner} />
        <span className={styles.loadingText}>Loading dashboard...</span>
      </div>
    )
  }

  // Error state
  if (error && !data) {
    return (
      <div className={styles.errorContainer}>
        <span className={styles.errorTitle}>Unable to load dashboard</span>
        <span className={styles.errorMessage}>{error}</span>
        <button
          type="button"
          className={styles.retryButton}
          onClick={refresh}
        >
          Try again
        </button>
      </div>
    )
  }

  return (
    <div
      ref={containerRef}
      className={styles.dashboard}
      style={{ paddingTop: safeArea.top }}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      {/* Pull-to-refresh indicator */}
      <div
        className={`${styles.pullIndicator} ${isRefreshing ? styles.refreshing : ''}`}
        style={{
          height: pullDistance,
          opacity: pullProgress
        }}
      >
        <div
          className={styles.refreshIconWrapper}
          style={{
            transform: `rotate(${isRefreshing ? 0 : refreshRotation}deg)`
          }}
        >
          <RefreshIcon />
        </div>
        <span className={styles.pullText}>
          {isRefreshing
            ? 'Refreshing...'
            : pullDistance >= PULL_THRESHOLD
            ? 'Release to refresh'
            : 'Pull to refresh'}
        </span>
      </div>

      {/* Offline indicator */}
      {isOffline && (
        <div className={styles.offlineIndicator}>
          <OfflineIcon />
          <span>You're offline. Showing cached data.</span>
        </div>
      )}

      {/* Greeting section */}
      <header className={styles.greeting}>
        <h1 className={styles.greetingText}>
          {data?.greeting}, {data?.userName?.split(' ')[0]}
        </h1>
        <span className={styles.dateText}>{data?.currentDate}</span>
        {lastUpdated && !isRefreshing && (
          <span className={styles.lastUpdated}>{formatLastUpdated()}</span>
        )}
      </header>

      {/* Alert banner */}
      {data?.alerts && data.alerts.length > 0 && (
        <AlertBanner alerts={data.alerts} onDismiss={dismissAlert} />
      )}

      {/* Quick actions */}
      <QuickActions />

      {/* Today's priorities */}
      {data && (
        <TodaysPriorities
          tasks={data.todaysTasks}
          onComplete={completeTask}
          onUndo={undoCompleteTask}
          totalTasksCount={data.stats.tasksDueToday}
        />
      )}

      {/* Active orders */}
      {data && <ActiveOrders orders={data.activeOrders} />}

      {/* Recent activity */}
      {data && <RecentActivity activities={data.recentActivity} />}

      {/* Bottom padding for nav */}
      <div className={styles.bottomPadding} />
    </div>
  )
}

export default MobileDashboard
