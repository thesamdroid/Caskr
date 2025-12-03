/**
 * TaskItem Component
 *
 * Swipeable task item with velocity-based gesture detection.
 * Swipe right to complete, swipe left to delete.
 */

import React, { useRef, useState, useCallback, memo } from 'react'
import { MobileTask, SWIPE_THRESHOLD, VELOCITY_THRESHOLD } from '../../types'
import styles from './TaskItem.module.css'

export interface TaskItemProps {
  task: MobileTask
  onComplete: (taskId: number) => void
  onDelete: (taskId: number) => void
  onPress: (task: MobileTask) => void
  onLongPress: (task: MobileTask) => void
  isSelected?: boolean
  isMultiSelectMode?: boolean
  style?: React.CSSProperties
}

interface TouchState {
  startX: number
  startY: number
  startTime: number
  currentX: number
  isDragging: boolean
  direction: 'left' | 'right' | null
}

const initialTouchState: TouchState = {
  startX: 0,
  startY: 0,
  startTime: 0,
  currentX: 0,
  isDragging: false,
  direction: null,
}

function TaskItemComponent({
  task,
  onComplete,
  onDelete,
  onPress,
  onLongPress,
  isSelected = false,
  isMultiSelectMode = false,
  style,
}: TaskItemProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const touchStateRef = useRef<TouchState>(initialTouchState)
  const longPressTimerRef = useRef<NodeJS.Timeout | null>(null)
  const [translateX, setTranslateX] = useState(0)
  const [isAnimating, setIsAnimating] = useState(false)
  const [isCompleting, setIsCompleting] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const prefersReducedMotion = useRef(
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  )

  const getItemWidth = useCallback(() => {
    return containerRef.current?.offsetWidth || 300
  }, [])

  const clearLongPressTimer = useCallback(() => {
    if (longPressTimerRef.current) {
      clearTimeout(longPressTimerRef.current)
      longPressTimerRef.current = null
    }
  }, [])

  const handleTouchStart = useCallback(
    (e: React.TouchEvent) => {
      if (isAnimating || task.isComplete) return

      const touch = e.touches[0]
      touchStateRef.current = {
        startX: touch.clientX,
        startY: touch.clientY,
        startTime: Date.now(),
        currentX: touch.clientX,
        isDragging: false,
        direction: null,
      }

      // Start long press timer
      longPressTimerRef.current = setTimeout(() => {
        if (!touchStateRef.current.isDragging) {
          // Haptic feedback for long press
          if (navigator.vibrate) {
            navigator.vibrate(50)
          }
          onLongPress(task)
        }
      }, 500)
    },
    [isAnimating, task, onLongPress]
  )

  const handleTouchMove = useCallback(
    (e: React.TouchEvent) => {
      if (isAnimating || task.isComplete) return

      const touch = e.touches[0]
      const deltaX = touch.clientX - touchStateRef.current.startX
      const deltaY = touch.clientY - touchStateRef.current.startY

      // If moving more vertically, don't handle swipe
      if (!touchStateRef.current.isDragging && Math.abs(deltaY) > Math.abs(deltaX)) {
        clearLongPressTimer()
        return
      }

      // Threshold to start dragging
      if (!touchStateRef.current.isDragging && Math.abs(deltaX) > 10) {
        touchStateRef.current.isDragging = true
        touchStateRef.current.direction = deltaX > 0 ? 'right' : 'left'
        clearLongPressTimer()
      }

      if (touchStateRef.current.isDragging) {
        touchStateRef.current.currentX = touch.clientX

        // Apply resistance at the edges
        const itemWidth = getItemWidth()
        const maxSwipe = itemWidth * 0.6
        let newTranslateX = deltaX

        if (Math.abs(newTranslateX) > maxSwipe) {
          const overflow = Math.abs(newTranslateX) - maxSwipe
          const resistance = 1 - overflow / (overflow + 100)
          newTranslateX =
            (newTranslateX > 0 ? 1 : -1) * (maxSwipe + overflow * resistance)
        }

        setTranslateX(newTranslateX)
      }
    },
    [isAnimating, task.isComplete, getItemWidth, clearLongPressTimer]
  )

  const handleTouchEnd = useCallback(() => {
    clearLongPressTimer()

    if (!touchStateRef.current.isDragging) {
      // It's a tap, not a swipe
      if (!isAnimating && !task.isComplete) {
        if (isMultiSelectMode) {
          onLongPress(task) // Toggle selection in multi-select mode
        } else {
          onPress(task)
        }
      }
      touchStateRef.current = initialTouchState
      return
    }

    const itemWidth = getItemWidth()
    const deltaX = touchStateRef.current.currentX - touchStateRef.current.startX
    const deltaTime = Date.now() - touchStateRef.current.startTime
    const velocity = Math.abs(deltaX) / deltaTime

    const thresholdPx = itemWidth * SWIPE_THRESHOLD
    const shouldTrigger =
      Math.abs(deltaX) > thresholdPx || velocity > VELOCITY_THRESHOLD

    if (shouldTrigger) {
      // Haptic feedback
      if (navigator.vibrate) {
        navigator.vibrate(30)
      }

      if (deltaX > 0) {
        // Swipe right - complete
        triggerComplete()
      } else {
        // Swipe left - delete
        triggerDelete()
      }
    } else {
      // Spring back
      animateBack()
    }

    touchStateRef.current = initialTouchState
  }, [
    clearLongPressTimer,
    isAnimating,
    task,
    isMultiSelectMode,
    onLongPress,
    onPress,
    getItemWidth,
  ])

  const triggerComplete = useCallback(() => {
    setIsAnimating(true)
    setIsCompleting(true)

    const itemWidth = getItemWidth()

    if (prefersReducedMotion.current) {
      // Instant for reduced motion
      onComplete(task.id)
      setTranslateX(0)
      setIsAnimating(false)
      setIsCompleting(false)
    } else {
      // Animate off screen
      setTranslateX(itemWidth)

      setTimeout(() => {
        onComplete(task.id)
        setTranslateX(0)
        setIsAnimating(false)
        setIsCompleting(false)
      }, 300)
    }
  }, [getItemWidth, onComplete, task.id])

  const triggerDelete = useCallback(() => {
    setIsAnimating(true)
    setIsDeleting(true)

    const itemWidth = getItemWidth()

    if (prefersReducedMotion.current) {
      onDelete(task.id)
      setTranslateX(0)
      setIsAnimating(false)
      setIsDeleting(false)
    } else {
      setTranslateX(-itemWidth)

      setTimeout(() => {
        onDelete(task.id)
        setTranslateX(0)
        setIsAnimating(false)
        setIsDeleting(false)
      }, 300)
    }
  }, [getItemWidth, onDelete, task.id])

  const animateBack = useCallback(() => {
    setIsAnimating(true)
    setTranslateX(0)

    setTimeout(() => {
      setIsAnimating(false)
    }, 200)
  }, [])

  const getPriorityLabel = (priority: MobileTask['priority']) => {
    switch (priority) {
      case 'high':
        return 'High'
      case 'medium':
        return 'Medium'
      case 'low':
        return 'Low'
      default:
        return ''
    }
  }

  const formatDueDate = (dateStr: string, timeStr?: string) => {
    const date = new Date(dateStr)
    const today = new Date()
    const tomorrow = new Date(today)
    tomorrow.setDate(tomorrow.getDate() + 1)

    const isToday = date.toDateString() === today.toDateString()
    const isTomorrow = date.toDateString() === tomorrow.toDateString()
    const isPast = date < today && !isToday

    let label: string
    if (isToday) {
      label = 'Today'
    } else if (isTomorrow) {
      label = 'Tomorrow'
    } else if (isPast) {
      const days = Math.floor((today.getTime() - date.getTime()) / (1000 * 60 * 60 * 24))
      label = `${days} day${days > 1 ? 's' : ''} overdue`
    } else {
      label = date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    }

    if (timeStr) {
      label += ` at ${timeStr}`
    }

    return { label, isPast }
  }

  const dueInfo = formatDueDate(task.dueDate, task.dueTime)

  return (
    <div
      className={`${styles.wrapper} ${isCompleting ? styles.completing : ''} ${
        isDeleting ? styles.deleting : ''
      }`}
      style={style}
    >
      {/* Background action indicators */}
      <div className={styles.actionBackground}>
        <div className={`${styles.action} ${styles.completeAction}`}>
          <svg
            className={styles.actionIcon}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
          >
            <polyline points="20 6 9 17 4 12" />
          </svg>
          <span>Complete</span>
        </div>
        <div className={`${styles.action} ${styles.deleteAction}`}>
          <span>Delete</span>
          <svg
            className={styles.actionIcon}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
          >
            <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
          </svg>
        </div>
      </div>

      {/* Task content */}
      <div
        ref={containerRef}
        className={`${styles.container} ${isSelected ? styles.selected : ''} ${
          task.isComplete ? styles.completed : ''
        } ${isAnimating ? styles.animating : ''}`}
        style={{ transform: `translateX(${translateX}px)` }}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
        onTouchCancel={handleTouchEnd}
      >
        {isMultiSelectMode && (
          <div className={styles.checkbox}>
            <div className={`${styles.checkboxInner} ${isSelected ? styles.checked : ''}`}>
              {isSelected && (
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="3"
                >
                  <polyline points="20 6 9 17 4 12" />
                </svg>
              )}
            </div>
          </div>
        )}

        <div className={styles.content}>
          <div className={styles.header}>
            <span className={`${styles.priority} ${styles[task.priority]}`}>
              {getPriorityLabel(task.priority)}
            </span>
            {task.orderName && (
              <span className={styles.orderTag}>{task.orderName}</span>
            )}
          </div>

          <h3 className={styles.title}>{task.title}</h3>

          {task.description && (
            <p className={styles.description}>{task.description}</p>
          )}

          <div className={styles.meta}>
            <span className={`${styles.dueDate} ${dueInfo.isPast ? styles.overdue : ''}`}>
              <svg
                className={styles.metaIcon}
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                <line x1="16" y1="2" x2="16" y2="6" />
                <line x1="8" y1="2" x2="8" y2="6" />
                <line x1="3" y1="10" x2="21" y2="10" />
              </svg>
              {dueInfo.label}
            </span>

            {task.assigneeName && (
              <span className={styles.assignee}>
                {task.assigneeAvatar ? (
                  <img
                    src={task.assigneeAvatar}
                    alt=""
                    className={styles.avatar}
                  />
                ) : (
                  <svg
                    className={styles.metaIcon}
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                  >
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                )}
                {task.assigneeName}
              </span>
            )}

            {task.barrelSku && (
              <span className={styles.barrel}>
                <svg
                  className={styles.metaIcon}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <ellipse cx="12" cy="5" rx="9" ry="3" />
                  <path d="M21 5v14c0 1.66-4 3-9 3s-9-1.34-9-3V5" />
                  <path d="M3 12c0 1.66 4 3 9 3s9-1.34 9-3" />
                </svg>
                {task.barrelSku}
              </span>
            )}
          </div>
        </div>

        <svg
          className={styles.chevron}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <polyline points="9 18 15 12 9 6" />
        </svg>
      </div>
    </div>
  )
}

export const TaskItem = memo(TaskItemComponent)
