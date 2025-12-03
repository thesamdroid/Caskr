/**
 * NotificationPermission Component
 *
 * UI for requesting notification permission with explanation.
 */

import React, { useState, useCallback } from 'react'
import { usePushNotifications } from '../../push'
import styles from './NotificationPermission.module.css'

export interface NotificationPermissionProps {
  /** Callback when permission is granted */
  onGranted?: () => void
  /** Callback when permission is denied */
  onDenied?: () => void
  /** Callback when dismissed */
  onDismiss?: () => void
  /** Additional class name */
  className?: string
  /** Show as modal overlay */
  modal?: boolean
}

/**
 * Notification Permission Request Component
 */
export function NotificationPermission({
  onGranted,
  onDenied,
  onDismiss,
  className,
  modal = false,
}: NotificationPermissionProps): React.ReactElement | null {
  const { isSupported, permission, requestPermission, subscribe } = usePushNotifications()

  const [isRequesting, setIsRequesting] = useState(false)

  // Handle enable
  const handleEnable = useCallback(async () => {
    setIsRequesting(true)

    try {
      const perm = await requestPermission()

      if (perm === 'granted') {
        await subscribe()
        onGranted?.()
      } else {
        onDenied?.()
      }
    } catch {
      onDenied?.()
    } finally {
      setIsRequesting(false)
    }
  }, [requestPermission, subscribe, onGranted, onDenied])

  // Handle dismiss
  const handleDismiss = useCallback(() => {
    onDismiss?.()
  }, [onDismiss])

  // Don't show if not supported or already decided
  if (!isSupported || permission !== 'default') {
    return null
  }

  const content = (
    <div className={styles.card}>
      <div className={styles.iconWrapper}>
        <BellIcon />
      </div>

      <h3 className={styles.title}>Stay Updated</h3>

      <p className={styles.description}>
        Get notified when you're assigned tasks, when deadlines approach, and
        when compliance reports are due.
      </p>

      <ul className={styles.benefits}>
        <li>
          <CheckIcon />
          <span>Task assignments and updates</span>
        </li>
        <li>
          <CheckIcon />
          <span>Compliance deadline reminders</span>
        </li>
        <li>
          <CheckIcon />
          <span>Sync status for offline work</span>
        </li>
      </ul>

      <div className={styles.buttons}>
        <button
          className={styles.enableButton}
          onClick={handleEnable}
          disabled={isRequesting}
        >
          {isRequesting ? 'Enabling...' : 'Enable Notifications'}
        </button>
        <button className={styles.dismissButton} onClick={handleDismiss}>
          Not now
        </button>
      </div>

      <p className={styles.note}>
        You can change this anytime in Settings.
      </p>
    </div>
  )

  if (modal) {
    return (
      <div className={`${styles.overlay} ${className || ''}`} role="dialog" aria-modal="true">
        {content}
      </div>
    )
  }

  return <div className={className}>{content}</div>
}

// Icons
function BellIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
    </svg>
  )
}

function CheckIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20,6 9,17 4,12" />
    </svg>
  )
}

export default NotificationPermission
