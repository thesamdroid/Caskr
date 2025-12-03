import { useState, useRef, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import type { DashboardTask } from '../../types'
import styles from './TodaysPriorities.module.css'

export interface TodaysPrioritiesProps {
  tasks: DashboardTask[]
  onComplete: (taskId: number) => Promise<void>
  onUndo: (taskId: number) => Promise<void>
  totalTasksCount?: number
}

// Priority indicator colors
const priorityColors: Record<string, string> = {
  high: '#dc2626',
  medium: '#d97706',
  low: '#2563eb'
}

// Checkmark icon
const CheckIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.checkIcon}>
    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
  </svg>
)

// Celebration icon for empty state
const CelebrationIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.celebrationIcon}>
    <path d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z"/>
  </svg>
)

// Chevron icon
const ChevronIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.chevronIcon}>
    <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
  </svg>
)

interface TaskItemProps {
  task: DashboardTask
  onComplete: (taskId: number) => Promise<void>
  onExpand: (taskId: number) => void
  isExpanded: boolean
}

const SWIPE_THRESHOLD = 0.4 // 40% of item width
const VELOCITY_THRESHOLD = 0.5 // pixels per ms

function TaskItem({ task, onComplete, onExpand, isExpanded }: TaskItemProps) {
  const itemRef = useRef<HTMLDivElement>(null)
  const touchStartX = useRef<number | null>(null)
  const touchStartTime = useRef<number | null>(null)
  const touchCurrentX = useRef<number | null>(null)
  const [swipeOffset, setSwipeOffset] = useState(0)
  const [isCompleting, setIsCompleting] = useState(false)

  // Check for reduced motion preference
  const prefersReducedMotion = typeof window !== 'undefined' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (task.isComplete) return
    touchStartX.current = e.touches[0].clientX
    touchStartTime.current = Date.now()
    touchCurrentX.current = e.touches[0].clientX
  }, [task.isComplete])

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    if (touchStartX.current === null || task.isComplete) return
    touchCurrentX.current = e.touches[0].clientX
    const deltaX = touchCurrentX.current - touchStartX.current

    // Only allow right swipe (positive delta)
    if (deltaX > 0) {
      setSwipeOffset(deltaX)
    }
  }, [task.isComplete])

  const handleTouchEnd = useCallback(async () => {
    if (touchStartX.current === null || touchCurrentX.current === null || task.isComplete) {
      resetSwipe()
      return
    }

    const deltaX = touchCurrentX.current - touchStartX.current
    const deltaTime = Date.now() - (touchStartTime.current || 0)
    const velocity = Math.abs(deltaX) / deltaTime
    const itemWidth = itemRef.current?.offsetWidth || 300

    // Check if swipe exceeds threshold or is fast enough
    const shouldComplete =
      deltaX > itemWidth * SWIPE_THRESHOLD || velocity > VELOCITY_THRESHOLD

    if (shouldComplete && deltaX > 50) {
      setIsCompleting(true)

      // Haptic feedback
      if ('vibrate' in navigator) {
        navigator.vibrate(20)
      }

      try {
        await onComplete(task.id)
      } catch {
        setIsCompleting(false)
      }
    }

    resetSwipe()
  }, [task.id, task.isComplete, onComplete])

  const resetSwipe = () => {
    touchStartX.current = null
    touchStartTime.current = null
    touchCurrentX.current = null
    setSwipeOffset(0)
  }

  const handleClick = () => {
    if (swipeOffset === 0) {
      onExpand(task.id)
    }
  }

  const formatDueTime = (dateStr: string, timeStr?: string) => {
    if (timeStr) return timeStr
    const date = new Date(dateStr)
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
  }

  const itemStyle = {
    transform: swipeOffset > 0 ? `translateX(${swipeOffset}px)` : undefined,
    transition: swipeOffset === 0 && !prefersReducedMotion ? 'transform 0.2s ease-out' : 'none'
  }

  const revealStyle = {
    width: swipeOffset > 0 ? swipeOffset : 0,
    opacity: Math.min(swipeOffset / 100, 1)
  }

  if (isCompleting && !prefersReducedMotion) {
    return (
      <div className={`${styles.taskItem} ${styles.completing}`}>
        <div className={styles.completingContent}>
          <CheckIcon />
          <span>Task completed</span>
        </div>
      </div>
    )
  }

  if (task.isComplete) {
    return null
  }

  return (
    <div className={styles.taskItemWrapper} ref={itemRef}>
      {/* Swipe reveal background */}
      <div className={styles.swipeReveal} style={revealStyle}>
        <CheckIcon />
      </div>

      {/* Task content */}
      <div
        className={styles.taskItem}
        style={itemStyle}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
        onClick={handleClick}
        role="button"
        tabIndex={0}
        aria-label={`${task.title}, ${task.priority} priority, due ${formatDueTime(task.dueDate, task.dueTime)}`}
      >
        {/* Priority indicator */}
        <div
          className={styles.priorityIndicator}
          style={{ backgroundColor: priorityColors[task.priority] }}
          aria-label={`${task.priority} priority`}
        />

        {/* Task content */}
        <div className={styles.taskContent}>
          <span className={styles.taskTitle}>{task.title}</span>
          <div className={styles.taskMeta}>
            {task.orderName && (
              <span className={styles.orderRef}>{task.orderName}</span>
            )}
            {task.assigneeName && (
              <span className={styles.assignee}>{task.assigneeName}</span>
            )}
            <span className={styles.dueTime}>
              Due {formatDueTime(task.dueDate, task.dueTime)}
            </span>
          </div>

          {/* Expanded details */}
          {isExpanded && task.description && (
            <div className={styles.expandedDetails}>
              <p className={styles.description}>{task.description}</p>
            </div>
          )}
        </div>

        {/* Expand indicator */}
        <div className={`${styles.expandIndicator} ${isExpanded ? styles.expanded : ''}`}>
          <ChevronIcon />
        </div>
      </div>
    </div>
  )
}

/**
 * Today's priorities component showing tasks due today
 * with swipe-to-complete and tap-to-expand functionality
 */
export function TodaysPriorities({
  tasks,
  onComplete,
  onUndo,
  totalTasksCount
}: TodaysPrioritiesProps) {
  const navigate = useNavigate()
  const [expandedTaskId, setExpandedTaskId] = useState<number | null>(null)
  const [undoTaskId, setUndoTaskId] = useState<number | null>(null)
  const undoTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Sort by priority
  const sortedTasks = [...tasks]
    .filter(t => !t.isComplete)
    .sort((a, b) => {
      const priorityOrder: Record<string, number> = { high: 0, medium: 1, low: 2 }
      return priorityOrder[a.priority] - priorityOrder[b.priority]
    })
    .slice(0, 5) // Max 5 items

  const incompleteTasks = tasks.filter(t => !t.isComplete)
  const hasMoreTasks = (totalTasksCount ?? incompleteTasks.length) > 5

  const handleComplete = useCallback(async (taskId: number) => {
    // Clear any existing undo timeout
    if (undoTimeoutRef.current) {
      clearTimeout(undoTimeoutRef.current)
    }

    await onComplete(taskId)
    setUndoTaskId(taskId)

    // Auto-hide undo after 5 seconds
    undoTimeoutRef.current = setTimeout(() => {
      setUndoTaskId(null)
    }, 5000)
  }, [onComplete])

  const handleUndo = useCallback(async () => {
    if (undoTaskId === null) return

    if (undoTimeoutRef.current) {
      clearTimeout(undoTimeoutRef.current)
    }

    await onUndo(undoTaskId)
    setUndoTaskId(null)
  }, [undoTaskId, onUndo])

  const handleExpand = useCallback((taskId: number) => {
    setExpandedTaskId(prev => prev === taskId ? null : taskId)
  }, [])

  const handleViewAll = () => {
    navigate('/tasks')
  }

  // Empty state
  if (sortedTasks.length === 0) {
    return (
      <section className={styles.container} aria-label="Today's priorities">
        <h2 className={styles.sectionTitle}>Today's Priorities</h2>
        <div className={styles.emptyState}>
          <CelebrationIcon />
          <span className={styles.emptyTitle}>All caught up!</span>
          <span className={styles.emptySubtitle}>No tasks due today</span>
        </div>
      </section>
    )
  }

  return (
    <section className={styles.container} aria-label="Today's priorities">
      <h2 className={styles.sectionTitle}>Today's Priorities</h2>

      <div className={styles.taskList}>
        {sortedTasks.map(task => (
          <TaskItem
            key={task.id}
            task={task}
            onComplete={handleComplete}
            onExpand={handleExpand}
            isExpanded={expandedTaskId === task.id}
          />
        ))}
      </div>

      {hasMoreTasks && (
        <button
          type="button"
          className={styles.viewAllButton}
          onClick={handleViewAll}
        >
          View all {totalTasksCount ?? incompleteTasks.length} tasks
        </button>
      )}

      {/* Undo toast */}
      {undoTaskId !== null && (
        <div className={styles.undoToast} role="alert">
          <span>Task completed</span>
          <button
            type="button"
            className={styles.undoButton}
            onClick={handleUndo}
          >
            Undo
          </button>
        </div>
      )}
    </section>
  )
}

export default TodaysPriorities
