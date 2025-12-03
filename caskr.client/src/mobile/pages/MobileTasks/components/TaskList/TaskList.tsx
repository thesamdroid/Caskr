/**
 * TaskList Component
 *
 * Displays tasks grouped by due date with smooth scrolling.
 * Supports multi-select mode and pull-to-refresh.
 */

import React, { useCallback, useRef, useState, memo } from 'react'
import { TaskGroup, MobileTask } from '../../types'
import { TaskItem } from '../TaskItem'
import styles from './TaskList.module.css'

export interface TaskListProps {
  groups: TaskGroup[]
  isLoading: boolean
  error: string | null
  onComplete: (taskId: number) => void
  onDelete: (taskId: number) => void
  onTaskPress: (task: MobileTask) => void
  onTaskLongPress: (task: MobileTask) => void
  selectedTaskIds: Set<number>
  isMultiSelectMode: boolean
  onRefresh: () => Promise<void>
  isRefreshing: boolean
  emptyMessage?: string
}

const PULL_THRESHOLD = 80

function TaskListComponent({
  groups,
  isLoading,
  error,
  onComplete,
  onDelete,
  onTaskPress,
  onTaskLongPress,
  selectedTaskIds,
  isMultiSelectMode,
  onRefresh,
  isRefreshing,
  emptyMessage = 'No tasks found',
}: TaskListProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [pullDistance, setPullDistance] = useState(0)
  const [isPulling, setIsPulling] = useState(false)
  const touchStartY = useRef(0)
  const isAtTop = useRef(true)

  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const target = e.target as HTMLDivElement
    isAtTop.current = target.scrollTop === 0
  }, [])

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (isAtTop.current && !isRefreshing) {
      touchStartY.current = e.touches[0].clientY
      setIsPulling(true)
    }
  }, [isRefreshing])

  const handleTouchMove = useCallback(
    (e: React.TouchEvent) => {
      if (!isPulling || isRefreshing) return

      const deltaY = e.touches[0].clientY - touchStartY.current

      if (deltaY > 0 && isAtTop.current) {
        // Apply resistance
        const resistance = 1 - deltaY / (deltaY + 300)
        setPullDistance(deltaY * resistance)
      }
    },
    [isPulling, isRefreshing]
  )

  const handleTouchEnd = useCallback(() => {
    if (!isPulling) return

    if (pullDistance > PULL_THRESHOLD && !isRefreshing) {
      onRefresh()
    }

    setPullDistance(0)
    setIsPulling(false)
  }, [isPulling, pullDistance, isRefreshing, onRefresh])

  const getGroupLabel = (key: string): string => {
    switch (key) {
      case 'overdue':
        return 'Overdue'
      case 'today':
        return 'Today'
      case 'tomorrow':
        return 'Tomorrow'
      case 'thisWeek':
        return 'This Week'
      case 'later':
        return 'Later'
      default:
        return key
    }
  }

  const totalTasks = groups.reduce((sum, g) => sum + g.tasks.length, 0)
  const isEmpty = totalTasks === 0 && !isLoading

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <svg
          className={styles.errorIcon}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        <p className={styles.errorText}>{error}</p>
        <button className={styles.retryButton} onClick={() => onRefresh()}>
          Try Again
        </button>
      </div>
    )
  }

  return (
    <div
      ref={containerRef}
      className={styles.container}
      onScroll={handleScroll}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      onTouchCancel={handleTouchEnd}
    >
      {/* Pull to refresh indicator */}
      <div
        className={`${styles.pullIndicator} ${
          pullDistance > PULL_THRESHOLD ? styles.ready : ''
        } ${isRefreshing ? styles.refreshing : ''}`}
        style={{
          height: isRefreshing ? 60 : pullDistance,
          opacity: isRefreshing ? 1 : Math.min(pullDistance / PULL_THRESHOLD, 1),
        }}
      >
        {isRefreshing ? (
          <div className={styles.spinner} />
        ) : (
          <svg
            className={styles.pullIcon}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            style={{
              transform: `rotate(${Math.min(pullDistance / PULL_THRESHOLD, 1) * 180}deg)`,
            }}
          >
            <polyline points="7 13 12 18 17 13" />
            <line x1="12" y1="6" x2="12" y2="18" />
          </svg>
        )}
      </div>

      {isLoading && totalTasks === 0 ? (
        <div className={styles.loadingContainer}>
          <div className={styles.skeleton} />
          <div className={styles.skeleton} />
          <div className={styles.skeleton} />
          <div className={styles.skeleton} />
        </div>
      ) : isEmpty ? (
        <div className={styles.emptyContainer}>
          <svg
            className={styles.emptyIcon}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          >
            <path d="M9 11l3 3L22 4" />
            <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
          </svg>
          <p className={styles.emptyText}>{emptyMessage}</p>
        </div>
      ) : (
        <div className={styles.list}>
          {groups.map((group) => {
            if (group.tasks.length === 0) return null

            return (
              <div key={group.key} className={styles.group}>
                <div
                  className={`${styles.groupHeader} ${
                    group.key === 'overdue' ? styles.overdueHeader : ''
                  }`}
                >
                  <span className={styles.groupLabel}>
                    {getGroupLabel(group.key)}
                  </span>
                  <span className={styles.groupCount}>{group.tasks.length}</span>
                </div>

                <div className={styles.groupTasks}>
                  {group.tasks.map((task) => (
                    <TaskItem
                      key={task.id}
                      task={task}
                      onComplete={onComplete}
                      onDelete={onDelete}
                      onPress={onTaskPress}
                      onLongPress={onTaskLongPress}
                      isSelected={selectedTaskIds.has(task.id)}
                      isMultiSelectMode={isMultiSelectMode}
                    />
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

export const TaskList = memo(TaskListComponent)
