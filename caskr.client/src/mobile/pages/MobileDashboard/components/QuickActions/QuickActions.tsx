import { useNavigate } from 'react-router-dom'
import type { QuickActionType } from '../../types'
import styles from './QuickActions.module.css'

export interface QuickActionsProps {
  onActionClick?: (action: QuickActionType) => void
}

// Scan barrel icon
const ScanIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M4 4h4V2H2v6h2V4zm0 12H2v6h6v-2H4v-4zm16 4h-4v2h6v-6h-2v4zm0-16V2h-6v2h4v4h2V4zM9 9h6v6H9V9zm-2 8h10V7H7v10z"/>
  </svg>
)

// New task icon
const TaskIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
  </svg>
)

// Movement icon
const MovementIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
  </svg>
)

// Gauge icon
const GaugeIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.88-11.71L10 14.17l-1.88-1.88a.996.996 0 1 0-1.41 1.41l2.59 2.59c.39.39 1.02.39 1.41 0L17.29 9.7a.996.996 0 0 0 0-1.41c-.39-.38-1.03-.38-1.41 0z"/>
  </svg>
)

const quickActions = [
  {
    id: 'scan' as QuickActionType,
    label: 'Scan Barrel',
    icon: <ScanIcon />,
    route: '/scan'
  },
  {
    id: 'new-task' as QuickActionType,
    label: 'New Task',
    icon: <TaskIcon />,
    route: '/tasks/new'
  },
  {
    id: 'movement' as QuickActionType,
    label: 'Record Movement',
    icon: <MovementIcon />,
    route: '/barrels/movement'
  },
  {
    id: 'gauge' as QuickActionType,
    label: 'Log Gauge',
    icon: <GaugeIcon />,
    route: '/barrels/gauge'
  }
]

/**
 * Quick actions component with 4 large tap targets (80x80px)
 * for most common mobile actions
 */
export function QuickActions({ onActionClick }: QuickActionsProps) {
  const navigate = useNavigate()

  const handleActionClick = (action: typeof quickActions[0]) => {
    // Trigger haptic feedback if available
    if ('vibrate' in navigator) {
      navigator.vibrate(10)
    }

    if (onActionClick) {
      onActionClick(action.id)
    }

    navigate(action.route)
  }

  return (
    <section className={styles.container} aria-label="Quick actions">
      <div className={styles.grid}>
        {quickActions.map(action => (
          <button
            key={action.id}
            type="button"
            className={styles.actionButton}
            onClick={() => handleActionClick(action)}
            aria-label={action.label}
          >
            <div className={styles.iconWrapper}>
              {action.icon}
            </div>
            <span className={styles.label}>{action.label}</span>
          </button>
        ))}
      </div>
    </section>
  )
}

export default QuickActions
