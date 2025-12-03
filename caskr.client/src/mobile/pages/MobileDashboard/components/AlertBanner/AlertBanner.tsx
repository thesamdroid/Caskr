import { useNavigate } from 'react-router-dom'
import type { DashboardAlert, AlertType } from '../../types'
import styles from './AlertBanner.module.css'

export interface AlertBannerProps {
  alerts: DashboardAlert[]
  onDismiss: (alertId: string) => void
}

// Alert icons by type
const AlertIcon = ({ type }: { type: AlertType }) => {
  switch (type) {
    case 'critical':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
        </svg>
      )
    case 'warning':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
          <path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"/>
        </svg>
      )
    case 'info':
      return (
        <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"/>
        </svg>
      )
  }
}

// Close icon
const CloseIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.closeIcon}>
    <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
  </svg>
)

// Chevron icon
const ChevronIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.chevronIcon}>
    <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
  </svg>
)

/**
 * Alert banner component for displaying critical, warning, and info alerts
 * Alerts are displayed in priority order: critical > warning > info
 * Tappable to navigate to relevant page, dismissible with reappearance
 */
export function AlertBanner({ alerts, onDismiss }: AlertBannerProps) {
  const navigate = useNavigate()

  // Sort alerts by priority
  const sortedAlerts = [...alerts].sort((a, b) => {
    const priority: Record<AlertType, number> = { critical: 0, warning: 1, info: 2 }
    return priority[a.type] - priority[b.type]
  })

  // Only show the highest priority alert
  const topAlert = sortedAlerts[0]

  if (!topAlert) {
    return null
  }

  const handleTap = () => {
    if (topAlert.actionUrl) {
      navigate(topAlert.actionUrl)
    }
  }

  const handleDismiss = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (topAlert.dismissible) {
      onDismiss(topAlert.id)
    }
  }

  const getTypeClass = (type: AlertType) => {
    switch (type) {
      case 'critical':
        return styles.critical
      case 'warning':
        return styles.warning
      case 'info':
        return styles.info
    }
  }

  return (
    <div className={styles.container}>
      <button
        type="button"
        className={`${styles.alert} ${getTypeClass(topAlert.type)}`}
        onClick={handleTap}
        aria-label={`${topAlert.type} alert: ${topAlert.title}`}
      >
        <div className={styles.iconWrapper}>
          <AlertIcon type={topAlert.type} />
        </div>

        <div className={styles.content}>
          <span className={styles.title}>{topAlert.title}</span>
          <span className={styles.message}>{topAlert.message}</span>
        </div>

        {topAlert.actionUrl && (
          <div className={styles.chevronWrapper}>
            <ChevronIcon />
          </div>
        )}

        {topAlert.dismissible && (
          <button
            type="button"
            className={styles.dismissButton}
            onClick={handleDismiss}
            aria-label="Dismiss alert"
          >
            <CloseIcon />
          </button>
        )}
      </button>

      {/* Show count if more alerts */}
      {sortedAlerts.length > 1 && (
        <div className={styles.moreAlerts}>
          <span>+{sortedAlerts.length - 1} more {sortedAlerts.length - 1 === 1 ? 'alert' : 'alerts'}</span>
        </div>
      )}
    </div>
  )
}

export default AlertBanner
