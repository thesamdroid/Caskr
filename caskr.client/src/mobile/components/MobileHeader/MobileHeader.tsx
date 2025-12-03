import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSafeArea, useHeaderCollapse } from '../../hooks'
import styles from './MobileHeader.module.css'

export interface MobileHeaderProps {
  title: string
  subtitle?: string
  showBackButton?: boolean
  onBackClick?: () => void
  leftAction?: React.ReactNode
  rightActions?: React.ReactNode[]
  enableCollapse?: boolean
}

// Back arrow icon
const BackIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.actionIcon}>
    <path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/>
  </svg>
)

/**
 * Mobile header with collapse on scroll behavior
 */
export function MobileHeader({
  title,
  subtitle,
  showBackButton = false,
  onBackClick,
  leftAction,
  rightActions = [],
  enableCollapse = true
}: MobileHeaderProps) {
  const navigate = useNavigate()
  const safeArea = useSafeArea()
  const { isCollapsed, collapseProgress } = useHeaderCollapse({
    enabled: enableCollapse,
    threshold: 50,
    expandedHeight: 96,
    collapsedHeight: 56
  })

  const handleBackClick = () => {
    if (onBackClick) {
      onBackClick()
    } else {
      navigate(-1)
    }
  }

  // Calculate header styles based on collapse progress
  const headerStyle = useMemo(() => {
    const expandedHeight = 96
    const collapsedHeight = 56
    const height = expandedHeight - (expandedHeight - collapsedHeight) * collapseProgress

    return {
      height: height + safeArea.top,
      paddingTop: safeArea.top
    }
  }, [collapseProgress, safeArea.top])

  // Calculate title styles
  const titleStyle = useMemo(() => {
    // Title size scales from 24px to 18px
    const fontSize = 24 - (24 - 18) * collapseProgress
    // Title opacity for large title (fades out)
    const largeOpacity = 1 - collapseProgress

    return {
      fontSize,
      largeOpacity
    }
  }, [collapseProgress])

  return (
    <header className={styles.header} style={headerStyle}>
      <div className={`${styles.headerContent} ${isCollapsed ? styles.collapsed : ''}`}>
        {/* Left section - Back button or custom action */}
        <div className={styles.leftSection}>
          {showBackButton && !leftAction && (
            <button
              type="button"
              className={styles.actionButton}
              onClick={handleBackClick}
              aria-label="Go back"
            >
              <BackIcon />
            </button>
          )}
          {leftAction}
        </div>

        {/* Center section - Title */}
        <div className={styles.centerSection}>
          {/* Large title (shown when expanded) */}
          {!isCollapsed && (
            <div
              className={styles.largeTitle}
              style={{ opacity: titleStyle.largeOpacity }}
            >
              <h1 className={styles.titleText} style={{ fontSize: titleStyle.fontSize }}>
                {title}
              </h1>
              {subtitle && (
                <p className={styles.subtitle}>{subtitle}</p>
              )}
            </div>
          )}

          {/* Inline title (shown when collapsed) */}
          {isCollapsed && (
            <h1 className={styles.inlineTitle}>{title}</h1>
          )}
        </div>

        {/* Right section - Action buttons */}
        <div className={styles.rightSection}>
          {rightActions.map((action, index) => (
            <div key={index} className={styles.actionWrapper}>
              {action}
            </div>
          ))}
        </div>
      </div>
    </header>
  )
}

export default MobileHeader
