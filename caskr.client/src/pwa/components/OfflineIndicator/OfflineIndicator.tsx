/**
 * OfflineIndicator Component
 *
 * Displays offline status and pending sync count.
 */

import { useState, useEffect, useCallback, memo } from 'react'
import { subscribeToSyncStatus, forceSync, SyncStatus } from '../../sync/backgroundSync'
import styles from './OfflineIndicator.module.css'

export interface OfflineIndicatorProps {
  position?: 'top' | 'bottom'
  showSyncButton?: boolean
  className?: string
}

function OfflineIndicatorComponent({
  position = 'top',
  showSyncButton = true,
  className = '',
}: OfflineIndicatorProps) {
  const [isOnline, setIsOnline] = useState(navigator.onLine)
  const [syncStatus, setSyncStatus] = useState<SyncStatus | null>(null)
  const [isSyncing, setIsSyncing] = useState(false)
  const [showIndicator, setShowIndicator] = useState(false)

  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true)
      // Show briefly when coming back online
      setShowIndicator(true)
      setTimeout(() => setShowIndicator(false), 3000)
    }

    const handleOffline = () => {
      setIsOnline(false)
      setShowIndicator(true)
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    // Initial state
    if (!navigator.onLine) {
      setShowIndicator(true)
    }

    // Subscribe to sync status
    const unsubscribe = subscribeToSyncStatus((status) => {
      setSyncStatus(status)
      if (status.pendingCount > 0) {
        setShowIndicator(true)
      }
    })

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
      unsubscribe()
    }
  }, [])

  const handleSync = useCallback(async () => {
    if (!isOnline || isSyncing) return

    setIsSyncing(true)
    try {
      await forceSync()
    } catch (error) {
      console.error('[OfflineIndicator] Sync failed:', error)
    } finally {
      setIsSyncing(false)
    }
  }, [isOnline, isSyncing])

  const handleDismiss = useCallback(() => {
    if (isOnline && (!syncStatus || syncStatus.pendingCount === 0)) {
      setShowIndicator(false)
    }
  }, [isOnline, syncStatus])

  // Don't render if online and no pending actions
  if (!showIndicator) {
    return null
  }

  const hasPendingActions = syncStatus && syncStatus.pendingCount > 0

  return (
    <div
      className={`${styles.container} ${styles[position]} ${
        isOnline ? styles.online : styles.offline
      } ${className}`}
      role="status"
      aria-live="polite"
    >
      <div className={styles.content}>
        {/* Status icon */}
        <div className={styles.iconContainer}>
          {isOnline ? (
            hasPendingActions ? (
              // Syncing icon
              <svg
                className={`${styles.icon} ${isSyncing ? styles.spinning : ''}`}
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M21.5 2v6h-6M2.5 22v-6h6M2 12a10 10 0 0 1 16.5-7.5M22 12a10 10 0 0 1-16.5 7.5" />
              </svg>
            ) : (
              // Online check icon
              <svg
                className={styles.icon}
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
              >
                <polyline points="20 6 9 17 4 12" />
              </svg>
            )
          ) : (
            // Offline icon
            <svg
              className={styles.icon}
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <line x1="1" y1="1" x2="23" y2="23" />
              <path d="M16.72 11.06A10.94 10.94 0 0 1 19 12.55" />
              <path d="M5 12.55a10.94 10.94 0 0 1 5.17-2.39" />
              <path d="M10.71 5.05A16 16 0 0 1 22.58 9" />
              <path d="M1.42 9a15.91 15.91 0 0 1 4.7-2.88" />
              <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
              <line x1="12" y1="20" x2="12.01" y2="20" />
            </svg>
          )}
        </div>

        {/* Message */}
        <div className={styles.message}>
          {isOnline ? (
            hasPendingActions ? (
              <>
                <span className={styles.title}>
                  {isSyncing ? 'Syncing...' : 'Back online'}
                </span>
                <span className={styles.subtitle}>
                  {syncStatus.pendingCount} action{syncStatus.pendingCount > 1 ? 's' : ''}{' '}
                  pending
                </span>
              </>
            ) : (
              <span className={styles.title}>Back online</span>
            )
          ) : (
            <>
              <span className={styles.title}>You're offline</span>
              <span className={styles.subtitle}>
                {hasPendingActions
                  ? `${syncStatus.pendingCount} change${syncStatus.pendingCount > 1 ? 's' : ''} will sync when online`
                  : 'Changes will sync when online'}
              </span>
            </>
          )}
        </div>

        {/* Actions */}
        <div className={styles.actions}>
          {showSyncButton && isOnline && hasPendingActions && !isSyncing && (
            <button
              className={styles.syncButton}
              onClick={handleSync}
              aria-label="Sync now"
            >
              Sync
            </button>
          )}

          {isOnline && (
            <button
              className={styles.dismissButton}
              onClick={handleDismiss}
              aria-label="Dismiss"
            >
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

export const OfflineIndicator = memo(OfflineIndicatorComponent)
