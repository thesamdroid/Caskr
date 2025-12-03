import { useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import type { DashboardOrder } from '../../types'
import styles from './ActiveOrders.module.css'

export interface ActiveOrdersProps {
  orders: DashboardOrder[]
}

interface CircularProgressProps {
  progress: number
  size?: number
  strokeWidth?: number
}

/**
 * Circular progress indicator component
 */
function CircularProgress({ progress, size = 48, strokeWidth = 4 }: CircularProgressProps) {
  const radius = (size - strokeWidth) / 2
  const circumference = radius * 2 * Math.PI
  const strokeDashoffset = circumference - (progress / 100) * circumference

  return (
    <svg
      className={styles.progressRing}
      width={size}
      height={size}
      viewBox={`0 0 ${size} ${size}`}
    >
      {/* Background circle */}
      <circle
        className={styles.progressBackground}
        cx={size / 2}
        cy={size / 2}
        r={radius}
        strokeWidth={strokeWidth}
        fill="none"
      />
      {/* Progress circle */}
      <circle
        className={styles.progressValue}
        cx={size / 2}
        cy={size / 2}
        r={radius}
        strokeWidth={strokeWidth}
        fill="none"
        strokeDasharray={circumference}
        strokeDashoffset={strokeDashoffset}
        strokeLinecap="round"
        transform={`rotate(-90 ${size / 2} ${size / 2})`}
      />
      {/* Percentage text */}
      <text
        x="50%"
        y="50%"
        dominantBaseline="central"
        textAnchor="middle"
        className={styles.progressText}
      >
        {progress}%
      </text>
    </svg>
  )
}

// Barrel icon
const BarrelIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.barrelIcon}>
    <path d="M18 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-1 18H7V4h10v16zM8 6h8v2H8V6zm0 4h8v2H8v-2zm0 4h8v2H8v-2z"/>
  </svg>
)

// Empty state icon
const EmptyOrdersIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.emptyIcon}>
    <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zm-7-2h2v-4h4v-2h-4V7h-2v4H8v2h4z"/>
  </svg>
)

/**
 * Active orders component with horizontal scrolling carousel
 */
export function ActiveOrders({ orders }: ActiveOrdersProps) {
  const navigate = useNavigate()
  const scrollContainerRef = useRef<HTMLDivElement>(null)

  const handleOrderClick = (orderId: number) => {
    // Haptic feedback
    if ('vibrate' in navigator) {
      navigator.vibrate(10)
    }
    navigate(`/orders/${orderId}`)
  }

  // Empty state
  if (orders.length === 0) {
    return (
      <section className={styles.container} aria-label="Active orders">
        <h2 className={styles.sectionTitle}>Active Orders</h2>
        <div className={styles.emptyState}>
          <EmptyOrdersIcon />
          <span className={styles.emptyTitle}>No active orders</span>
          <span className={styles.emptySubtitle}>Orders in progress will appear here</span>
        </div>
      </section>
    )
  }

  return (
    <section className={styles.container} aria-label="Active orders">
      <div className={styles.header}>
        <h2 className={styles.sectionTitle}>Active Orders</h2>
        <span className={styles.orderCount}>{orders.length}</span>
      </div>

      <div
        ref={scrollContainerRef}
        className={styles.scrollContainer}
        role="list"
        aria-label="Order cards"
      >
        {orders.slice(0, 10).map((order) => (
          <button
            key={order.id}
            type="button"
            className={styles.orderCard}
            onClick={() => handleOrderClick(order.id)}
            role="listitem"
            aria-label={`${order.name}, ${order.progress}% complete, ${order.barrelCount} of ${order.totalBarrels} barrels`}
          >
            <div className={styles.cardHeader}>
              <span className={styles.orderName}>{order.name}</span>
              <span className={styles.status}>{order.status}</span>
            </div>

            <div className={styles.cardBody}>
              <CircularProgress progress={order.progress} />

              <div className={styles.barrelInfo}>
                <div className={styles.barrelCount}>
                  <BarrelIcon />
                  <span>
                    {order.barrelCount} / {order.totalBarrels}
                  </span>
                </div>
                <span className={styles.barrelLabel}>barrels</span>
              </div>
            </div>

            {order.dueDate && (
              <div className={styles.cardFooter}>
                <span className={styles.dueDate}>
                  Due {new Date(order.dueDate).toLocaleDateString('en-US', {
                    month: 'short',
                    day: 'numeric'
                  })}
                </span>
              </div>
            )}
          </button>
        ))}
      </div>
    </section>
  )
}

export default ActiveOrders
