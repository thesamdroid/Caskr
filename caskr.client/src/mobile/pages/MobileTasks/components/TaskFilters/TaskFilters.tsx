/**
 * TaskFilters Component
 *
 * Filter chips and sort controls for task list.
 */

import { useState, useCallback, memo } from 'react'
import { TaskFiltersState, TaskViewMode, TaskPriority, TaskSortBy } from '../../types'
import styles from './TaskFilters.module.css'

export interface TaskFiltersProps {
  filters: TaskFiltersState
  viewMode: TaskViewMode
  onFiltersChange: (filters: Partial<TaskFiltersState>) => void
  onViewModeChange: (mode: TaskViewMode) => void
  onResetFilters: () => void
  hasActiveFilters: boolean
  completedToday: number
  totalToday: number
}

function TaskFiltersComponent({
  filters,
  viewMode,
  onFiltersChange,
  onViewModeChange,
  onResetFilters,
  hasActiveFilters,
  completedToday,
  totalToday,
}: TaskFiltersProps) {
  const [isExpanded, setIsExpanded] = useState(false)

  const handleViewModeChange = useCallback(
    (mode: TaskViewMode) => {
      onViewModeChange(mode)
    },
    [onViewModeChange]
  )

  const handlePriorityChange = useCallback(
    (priority: TaskPriority | 'all') => {
      onFiltersChange({ priority })
    },
    [onFiltersChange]
  )

  const handleDueDateChange = useCallback(
    (dueDateRange: TaskFiltersState['dueDateRange']) => {
      onFiltersChange({ dueDateRange })
    },
    [onFiltersChange]
  )

  const handleSortChange = useCallback(
    (sortBy: TaskSortBy) => {
      onFiltersChange({
        sortBy,
        sortOrder:
          filters.sortBy === sortBy && filters.sortOrder === 'asc' ? 'desc' : 'asc',
      })
    },
    [filters.sortBy, filters.sortOrder, onFiltersChange]
  )

  const toggleExpanded = useCallback(() => {
    setIsExpanded((prev) => !prev)
  }, [])

  return (
    <div className={styles.container}>
      {/* Progress bar */}
      <div className={styles.progressSection}>
        <div className={styles.progressInfo}>
          <span className={styles.progressLabel}>Today's Progress</span>
          <span className={styles.progressCount}>
            {completedToday} / {totalToday}
          </span>
        </div>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{
              width: totalToday > 0 ? `${(completedToday / totalToday) * 100}%` : '0%',
            }}
          />
        </div>
      </div>

      {/* View mode tabs */}
      <div className={styles.viewModeSection}>
        <button
          className={`${styles.viewModeButton} ${viewMode === 'my' ? styles.active : ''}`}
          onClick={() => handleViewModeChange('my')}
        >
          My Tasks
        </button>
        <button
          className={`${styles.viewModeButton} ${viewMode === 'all' ? styles.active : ''}`}
          onClick={() => handleViewModeChange('all')}
        >
          All Tasks
        </button>
        <button
          className={`${styles.viewModeButton} ${
            viewMode === 'completed' ? styles.active : ''
          }`}
          onClick={() => handleViewModeChange('completed')}
        >
          Completed
        </button>
      </div>

      {/* Filter toggle */}
      <button className={styles.filterToggle} onClick={toggleExpanded}>
        <svg
          className={styles.filterIcon}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />
        </svg>
        <span>Filters</span>
        {hasActiveFilters && <span className={styles.activeIndicator} />}
        <svg
          className={`${styles.chevronIcon} ${isExpanded ? styles.rotated : ''}`}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>

      {/* Expanded filters */}
      {isExpanded && (
        <div className={styles.filtersExpanded}>
          {/* Priority filter */}
          <div className={styles.filterGroup}>
            <span className={styles.filterLabel}>Priority</span>
            <div className={styles.filterChips}>
              {(['all', 'high', 'medium', 'low'] as const).map((p) => (
                <button
                  key={p}
                  className={`${styles.chip} ${
                    filters.priority === p ? styles.chipActive : ''
                  } ${p !== 'all' ? styles[p] : ''}`}
                  onClick={() => handlePriorityChange(p)}
                >
                  {p === 'all' ? 'All' : p.charAt(0).toUpperCase() + p.slice(1)}
                </button>
              ))}
            </div>
          </div>

          {/* Due date filter */}
          <div className={styles.filterGroup}>
            <span className={styles.filterLabel}>Due Date</span>
            <div className={styles.filterChips}>
              {(
                [
                  { value: 'all', label: 'All' },
                  { value: 'overdue', label: 'Overdue' },
                  { value: 'today', label: 'Today' },
                  { value: 'thisWeek', label: 'This Week' },
                  { value: 'thisMonth', label: 'This Month' },
                ] as const
              ).map((item) => (
                <button
                  key={item.value}
                  className={`${styles.chip} ${
                    filters.dueDateRange === item.value ? styles.chipActive : ''
                  } ${item.value === 'overdue' ? styles.overdue : ''}`}
                  onClick={() => handleDueDateChange(item.value)}
                >
                  {item.label}
                </button>
              ))}
            </div>
          </div>

          {/* Sort by */}
          <div className={styles.filterGroup}>
            <span className={styles.filterLabel}>Sort By</span>
            <div className={styles.filterChips}>
              {(
                [
                  { value: 'dueDate', label: 'Due Date' },
                  { value: 'priority', label: 'Priority' },
                  { value: 'createdAt', label: 'Created' },
                  { value: 'alphabetical', label: 'A-Z' },
                ] as const
              ).map((item) => (
                <button
                  key={item.value}
                  className={`${styles.chip} ${styles.sortChip} ${
                    filters.sortBy === item.value ? styles.chipActive : ''
                  }`}
                  onClick={() => handleSortChange(item.value)}
                >
                  {item.label}
                  {filters.sortBy === item.value && (
                    <svg
                      className={`${styles.sortArrow} ${
                        filters.sortOrder === 'desc' ? styles.desc : ''
                      }`}
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2.5"
                    >
                      <polyline points="18 15 12 9 6 15" />
                    </svg>
                  )}
                </button>
              ))}
            </div>
          </div>

          {/* Reset button */}
          {hasActiveFilters && (
            <button className={styles.resetButton} onClick={onResetFilters}>
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
                <path d="M3 3v5h5" />
              </svg>
              Reset Filters
            </button>
          )}
        </div>
      )}
    </div>
  )
}

export const TaskFilters = memo(TaskFiltersComponent)
