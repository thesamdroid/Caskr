import { useCallback } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useSafeArea, useBottomNavHeight } from '../../hooks'
import styles from './BottomNav.module.css'

export interface NavItem {
  id: string
  label: string
  path: string
  icon: React.ReactNode
  badge?: number
  isMore?: boolean
}

export interface BottomNavProps {
  items: NavItem[]
  onMoreClick?: () => void
  tasksCount?: number
}

// Default icons as SVG components
const HomeIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>
  </svg>
)

const ScanIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M9.5 6.5v3h-3v-3h3M11 5H5v6h6V5zm-1.5 9.5v3h-3v-3h3M11 13H5v6h6v-6zm6.5-6.5v3h-3v-3h3M19 5h-6v6h6V5zm-6 8h1.5v1.5H13V13zm1.5 1.5H16V16h-1.5v-1.5zM16 13h1.5v1.5H16V13zm-3 3h1.5v1.5H13V16zm1.5 1.5H16V19h-1.5v-1.5zM16 16h1.5v1.5H16V16zm1.5-1.5H19V16h-1.5v-1.5zm0 3H19V19h-1.5v-1.5zM19 13v1.5h-1.5V13H19z"/>
  </svg>
)

const TasksIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
  </svg>
)

const BarrelIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <ellipse cx="12" cy="5" rx="8" ry="3"/>
    <path d="M4 5v14c0 1.66 3.58 3 8 3s8-1.34 8-3V5c0 1.66-3.58 3-8 3S4 6.66 4 5z"/>
    <path d="M4 9c0 1.66 3.58 3 8 3s8-1.34 8-3" fill="none" stroke="currentColor" strokeWidth="1"/>
    <path d="M4 14c0 1.66 3.58 3 8 3s8-1.34 8-3" fill="none" stroke="currentColor" strokeWidth="1"/>
  </svg>
)

const MoreIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.icon}>
    <path d="M3 18h18v-2H3v2zm0-5h18v-2H3v2zm0-7v2h18V6H3z"/>
  </svg>
)

/**
 * Default navigation items for the bottom nav
 */
export const defaultNavItems: NavItem[] = [
  { id: 'home', label: 'Home', path: '/', icon: <HomeIcon /> },
  { id: 'scan', label: 'Scan', path: '/scan', icon: <ScanIcon /> },
  { id: 'tasks', label: 'Tasks', path: '/tasks', icon: <TasksIcon /> },
  { id: 'barrels', label: 'Barrels', path: '/barrels', icon: <BarrelIcon /> },
  { id: 'more', label: 'More', path: '', icon: <MoreIcon />, isMore: true }
]

/**
 * Bottom navigation component for mobile app
 */
export function BottomNav({
  items = defaultNavItems,
  onMoreClick,
  tasksCount
}: BottomNavProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const safeArea = useSafeArea()
  const { totalHeight } = useBottomNavHeight()

  const isActive = useCallback((path: string): boolean => {
    if (path === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(path)
  }, [location.pathname])

  const handleItemClick = useCallback((item: NavItem) => {
    if (item.isMore) {
      onMoreClick?.()
      return
    }

    // Trigger haptic feedback if available
    if ('vibrate' in navigator) {
      navigator.vibrate(10)
    }

    navigate(item.path)
  }, [navigate, onMoreClick])

  // Calculate badge for tasks if not explicitly provided
  const getItemBadge = useCallback((item: NavItem): number | undefined => {
    if (item.id === 'tasks' && tasksCount !== undefined) {
      return tasksCount
    }
    return item.badge
  }, [tasksCount])

  return (
    <nav
      className={styles.bottomNav}
      style={{
        height: totalHeight,
        paddingBottom: safeArea.bottom
      }}
      role="navigation"
      aria-label="Primary mobile navigation"
    >
      <ul className={styles.navList}>
        {items.map((item) => {
          const active = !item.isMore && isActive(item.path)
          const badge = getItemBadge(item)

          return (
            <li key={item.id} className={styles.navItemWrapper}>
              <button
                type="button"
                className={`${styles.navItem} ${active ? styles.active : ''}`}
                onClick={() => handleItemClick(item)}
                aria-current={active ? 'page' : undefined}
                aria-label={badge ? `${item.label}, ${badge} notifications` : item.label}
              >
                <span className={styles.iconWrapper}>
                  {item.icon}
                  {badge !== undefined && badge > 0 && (
                    <span className={styles.badge} aria-hidden="true">
                      {badge > 99 ? '99+' : badge}
                    </span>
                  )}
                </span>
                <span className={styles.label}>{item.label}</span>
              </button>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}

export default BottomNav
