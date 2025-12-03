/**
 * TaskDetailSheet Component
 *
 * Bottom sheet modal for viewing task details with actions.
 */

import React, { useCallback, useEffect, useRef, useState, memo } from 'react'
import { MobileTask } from '../../types'
import styles from './TaskDetailSheet.module.css'

export interface TaskDetailSheetProps {
  task: MobileTask | null
  isOpen: boolean
  onClose: () => void
  onComplete: (taskId: number) => void
  onUncomplete: (taskId: number) => void
  onDelete: (taskId: number) => void
  onEdit: (task: MobileTask) => void
}

function TaskDetailSheetComponent({
  task,
  isOpen,
  onClose,
  onComplete,
  onUncomplete,
  onDelete,
  onEdit,
}: TaskDetailSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null)
  const [translateY, setTranslateY] = useState(0)
  const [isDragging, setIsDragging] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const touchStartY = useRef(0)
  const currentTranslateY = useRef(0)

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden'
      setShowDeleteConfirm(false)
    } else {
      document.body.style.overflow = ''
    }

    return () => {
      document.body.style.overflow = ''
    }
  }, [isOpen])

  const handleBackdropClick = useCallback(() => {
    onClose()
  }, [onClose])

  const handleDragStart = useCallback((e: React.TouchEvent) => {
    touchStartY.current = e.touches[0].clientY
    currentTranslateY.current = 0
    setIsDragging(true)
  }, [])

  const handleDragMove = useCallback((e: React.TouchEvent) => {
    if (!isDragging) return

    const deltaY = e.touches[0].clientY - touchStartY.current

    if (deltaY > 0) {
      currentTranslateY.current = deltaY
      setTranslateY(deltaY)
    }
  }, [isDragging])

  const handleDragEnd = useCallback(() => {
    setIsDragging(false)

    const sheetHeight = sheetRef.current?.offsetHeight || 400

    if (currentTranslateY.current > sheetHeight * 0.3) {
      onClose()
    }

    setTranslateY(0)
    currentTranslateY.current = 0
  }, [onClose])

  const handleComplete = useCallback(() => {
    if (!task) return
    if (task.isComplete) {
      onUncomplete(task.id)
    } else {
      onComplete(task.id)
    }
    onClose()
  }, [task, onComplete, onUncomplete, onClose])

  const handleEdit = useCallback(() => {
    if (!task) return
    onEdit(task)
  }, [task, onEdit])

  const handleDelete = useCallback(() => {
    if (!task) return
    if (!showDeleteConfirm) {
      setShowDeleteConfirm(true)
      return
    }
    onDelete(task.id)
    onClose()
  }, [task, showDeleteConfirm, onDelete, onClose])

  const getPriorityLabel = (priority: MobileTask['priority']) => {
    switch (priority) {
      case 'high':
        return 'High Priority'
      case 'medium':
        return 'Medium Priority'
      case 'low':
        return 'Low Priority'
      default:
        return ''
    }
  }

  const formatDate = (dateStr: string, timeStr?: string) => {
    const date = new Date(dateStr)
    const formatted = date.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    })

    if (timeStr) {
      return `${formatted} at ${timeStr}`
    }
    return formatted
  }

  if (!task) return null

  return (
    <div
      className={`${styles.overlay} ${isOpen ? styles.open : ''}`}
      onClick={handleBackdropClick}
    >
      <div
        ref={sheetRef}
        className={`${styles.sheet} ${isDragging ? styles.dragging : ''}`}
        style={{ transform: `translateY(${translateY}px)` }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Drag handle */}
        <div
          className={styles.dragHandle}
          onTouchStart={handleDragStart}
          onTouchMove={handleDragMove}
          onTouchEnd={handleDragEnd}
          onTouchCancel={handleDragEnd}
        >
          <div className={styles.handleBar} />
        </div>

        {/* Header */}
        <div className={styles.header}>
          <div className={`${styles.priority} ${styles[task.priority]}`}>
            {getPriorityLabel(task.priority)}
          </div>
          {task.isComplete && (
            <span className={styles.completedBadge}>Completed</span>
          )}
        </div>

        {/* Content */}
        <div className={styles.content}>
          <h2 className={styles.title}>{task.title}</h2>

          {task.description && (
            <p className={styles.description}>{task.description}</p>
          )}

          <div className={styles.details}>
            <div className={styles.detailRow}>
              <svg
                className={styles.detailIcon}
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
              <div className={styles.detailContent}>
                <span className={styles.detailLabel}>Due Date</span>
                <span className={styles.detailValue}>
                  {formatDate(task.dueDate, task.dueTime)}
                </span>
              </div>
            </div>

            {task.assigneeName && (
              <div className={styles.detailRow}>
                <svg
                  className={styles.detailIcon}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
                <div className={styles.detailContent}>
                  <span className={styles.detailLabel}>Assigned to</span>
                  <span className={styles.detailValue}>{task.assigneeName}</span>
                </div>
              </div>
            )}

            {task.orderName && (
              <div className={styles.detailRow}>
                <svg
                  className={styles.detailIcon}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                  <line x1="16" y1="13" x2="8" y2="13" />
                  <line x1="16" y1="17" x2="8" y2="17" />
                  <polyline points="10 9 9 9 8 9" />
                </svg>
                <div className={styles.detailContent}>
                  <span className={styles.detailLabel}>Order</span>
                  <span className={styles.detailValue}>{task.orderName}</span>
                </div>
              </div>
            )}

            {task.barrelSku && (
              <div className={styles.detailRow}>
                <svg
                  className={styles.detailIcon}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <ellipse cx="12" cy="5" rx="9" ry="3" />
                  <path d="M21 5v14c0 1.66-4 3-9 3s-9-1.34-9-3V5" />
                  <path d="M3 12c0 1.66 4 3 9 3s9-1.34 9-3" />
                </svg>
                <div className={styles.detailContent}>
                  <span className={styles.detailLabel}>Barrel</span>
                  <span className={styles.detailValue}>{task.barrelSku}</span>
                </div>
              </div>
            )}

            {task.createdBy && (
              <div className={styles.detailRow}>
                <svg
                  className={styles.detailIcon}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <circle cx="12" cy="12" r="10" />
                  <polyline points="12 6 12 12 16 14" />
                </svg>
                <div className={styles.detailContent}>
                  <span className={styles.detailLabel}>Created by</span>
                  <span className={styles.detailValue}>
                    {task.createdBy} on{' '}
                    {new Date(task.createdAt).toLocaleDateString()}
                  </span>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className={styles.actions}>
          <button
            className={`${styles.actionButton} ${styles.completeButton} ${
              task.isComplete ? styles.uncomplete : ''
            }`}
            onClick={handleComplete}
          >
            {task.isComplete ? (
              <>
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
                  <path d="M3 3v5h5" />
                </svg>
                Reopen Task
              </>
            ) : (
              <>
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                Mark Complete
              </>
            )}
          </button>

          <div className={styles.secondaryActions}>
            <button
              className={`${styles.actionButton} ${styles.editButton}`}
              onClick={handleEdit}
            >
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
              Edit
            </button>

            <button
              className={`${styles.actionButton} ${styles.deleteButton} ${
                showDeleteConfirm ? styles.confirm : ''
              }`}
              onClick={handleDelete}
            >
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
              </svg>
              {showDeleteConfirm ? 'Confirm' : 'Delete'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export const TaskDetailSheet = memo(TaskDetailSheetComponent)
