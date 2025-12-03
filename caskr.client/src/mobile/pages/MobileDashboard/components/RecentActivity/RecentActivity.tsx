import { useState } from 'react'
import type { DashboardActivity } from '../../types'
import styles from './RecentActivity.module.css'

export interface RecentActivityProps {
  activities: DashboardActivity[]
}

// Activity type icons
const BarrelIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.activityTypeIcon}>
    <path d="M18 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-1 18H7V4h10v16zM8 6h8v2H8V6zm0 4h8v2H8v-2z"/>
  </svg>
)

const TaskIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.activityTypeIcon}>
    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
  </svg>
)

const MovementIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.activityTypeIcon}>
    <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
  </svg>
)

const GaugeIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.activityTypeIcon}>
    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.88-11.71L10 14.17l-1.88-1.88a.996.996 0 1 0-1.41 1.41l2.59 2.59c.39.39 1.02.39 1.41 0L17.29 9.7a.996.996 0 0 0 0-1.41c-.39-.38-1.03-.38-1.41 0z"/>
  </svg>
)

const OrderIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.activityTypeIcon}>
    <path d="M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
  </svg>
)

// Chevron icons
const ChevronDownIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.chevronIcon}>
    <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/>
  </svg>
)

const ChevronUpIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.chevronIcon}>
    <path d="M7.41 15.41L12 10.83l4.59 4.58L18 14l-6-6-6 6 1.41 1.41z"/>
  </svg>
)

const getActivityIcon = (iconType: DashboardActivity['iconType']) => {
  switch (iconType) {
    case 'barrel':
      return <BarrelIcon />
    case 'task':
      return <TaskIcon />
    case 'movement':
      return <MovementIcon />
    case 'gauge':
      return <GaugeIcon />
    case 'order':
      return <OrderIcon />
    default:
      return <BarrelIcon />
  }
}

const getIconColorClass = (iconType: DashboardActivity['iconType']) => {
  switch (iconType) {
    case 'barrel':
      return styles.iconBarrel
    case 'task':
      return styles.iconTask
    case 'movement':
      return styles.iconMovement
    case 'gauge':
      return styles.iconGauge
    case 'order':
      return styles.iconOrder
    default:
      return styles.iconBarrel
  }
}

/**
 * Format relative time for activity timestamps
 */
function formatRelativeTime(timestamp: string): string {
  const date = new Date(timestamp)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / (1000 * 60))
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric'
  })
}

/**
 * Recent activity component with collapsible list
 */
export function RecentActivity({ activities }: RecentActivityProps) {
  const [isCollapsed, setIsCollapsed] = useState(false)

  const toggleCollapse = () => {
    setIsCollapsed(prev => !prev)
  }

  // Empty state
  if (activities.length === 0) {
    return null
  }

  return (
    <section className={styles.container} aria-label="Recent activity">
      <button
        type="button"
        className={styles.header}
        onClick={toggleCollapse}
        aria-expanded={!isCollapsed}
        aria-controls="activity-list"
      >
        <h2 className={styles.sectionTitle}>Recent Activity</h2>
        <div className={styles.collapseIndicator}>
          {isCollapsed ? <ChevronDownIcon /> : <ChevronUpIcon />}
        </div>
      </button>

      {!isCollapsed && (
        <div id="activity-list" className={styles.activityList}>
          {activities.slice(0, 5).map((activity) => (
            <div key={activity.id} className={styles.activityItem}>
              <div className={`${styles.iconWrapper} ${getIconColorClass(activity.iconType)}`}>
                {getActivityIcon(activity.iconType)}
              </div>

              <div className={styles.activityContent}>
                <span className={styles.activityTitle}>{activity.title}</span>
                {activity.description && (
                  <span className={styles.activityDescription}>{activity.description}</span>
                )}
                <span className={styles.activityTime}>
                  {formatRelativeTime(activity.timestamp)}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}

export default RecentActivity
