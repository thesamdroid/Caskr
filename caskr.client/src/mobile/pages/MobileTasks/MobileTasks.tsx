/**
 * MobileTasks Page
 *
 * Mobile task management with swipe gestures, filtering,
 * and offline support.
 */

import { useCallback, useState, useEffect } from 'react'
import { useTasksState } from './useTasksState'
import { TaskList, TaskFilters, TaskDetailSheet, TaskFormSheet } from './components'
import { MobileTask, CreateTaskData, UpdateTaskData, UNDO_TIMEOUT_MS } from './types'
import styles from './MobileTasks.module.css'

/**
 * Mobile tasks page with full task management capabilities
 */
export function MobileTasks() {
  const {
    groupedTasks,
    isLoading,
    error,
    viewMode,
    setViewMode,
    filters,
    setFilters,
    resetFilters,
    hasActiveFilters,
    completedToday,
    totalToday,
    selectedTaskIds,
    isMultiSelectMode,
    toggleMultiSelect,
    selectAll,
    clearSelection,
    completeTask,
    uncompleteTask,
    deleteTask,
    bulkComplete,
    bulkDelete,
    undoAction,
    canUndo,
    undoLabel,
    selectedTask,
    selectTask,
    refresh,
    isRefreshing,
    isOffline,
    pendingActions,
  } = useTasksState()

  const [showFormSheet, setShowFormSheet] = useState(false)
  const [editingTask, setEditingTask] = useState<MobileTask | null>(null)
  const [showUndoToast, setShowUndoToast] = useState(false)

  // Show undo toast when action is available
  useEffect(() => {
    if (canUndo) {
      setShowUndoToast(true)
      const timer = setTimeout(() => {
        setShowUndoToast(false)
      }, UNDO_TIMEOUT_MS)
      return () => clearTimeout(timer)
    } else {
      setShowUndoToast(false)
    }
  }, [canUndo])

  const handleTaskPress = useCallback(
    (task: MobileTask) => {
      if (isMultiSelectMode) {
        toggleMultiSelect(task.id)
      } else {
        selectTask(task.id)
      }
    },
    [isMultiSelectMode, toggleMultiSelect, selectTask]
  )

  const handleTaskLongPress = useCallback(
    (task: MobileTask) => {
      toggleMultiSelect(task.id)
    },
    [toggleMultiSelect]
  )

  const handleCloseDetail = useCallback(() => {
    selectTask(null)
  }, [selectTask])

  const handleEditFromDetail = useCallback(
    (task: MobileTask) => {
      selectTask(null)
      setEditingTask(task)
      setShowFormSheet(true)
    },
    [selectTask]
  )

  const handleOpenNewForm = useCallback(() => {
    setEditingTask(null)
    setShowFormSheet(true)
  }, [])

  const handleCloseForm = useCallback(() => {
    setShowFormSheet(false)
    setEditingTask(null)
  }, [])

  const handleSaveTask = useCallback(
    async (data: CreateTaskData | UpdateTaskData) => {
      // In a real implementation, this would call the API
      console.log('Save task:', data, editingTask?.id)
      // Refresh to get updated list
      await refresh()
    },
    [editingTask, refresh]
  )

  const handleBulkComplete = useCallback(async () => {
    const ids = Array.from(selectedTaskIds)
    await bulkComplete(ids)
    clearSelection()
  }, [selectedTaskIds, bulkComplete, clearSelection])

  const handleBulkDelete = useCallback(async () => {
    const ids = Array.from(selectedTaskIds)
    await bulkDelete(ids)
    clearSelection()
  }, [selectedTaskIds, bulkDelete, clearSelection])

  const handleUndo = useCallback(async () => {
    await undoAction()
    setShowUndoToast(false)
  }, [undoAction])

  return (
    <div className={styles.container}>
      {/* Offline indicator */}
      {isOffline && (
        <div className={styles.offlineIndicator}>
          <svg
            className={styles.offlineIcon}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
          >
            <line x1="1" y1="1" x2="23" y2="23" />
            <path d="M16.72 11.06A10.94 10.94 0 0 1 19 12.55" />
            <path d="M5 12.55a10.94 10.94 0 0 1 5.17-2.39" />
            <path d="M10.71 5.05A16 16 0 0 1 22.58 9" />
            <path d="M1.42 9a15.91 15.91 0 0 1 4.7-2.88" />
            <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
            <line x1="12" y1="20" x2="12.01" y2="20" />
          </svg>
          <span>
            Offline mode
            {pendingActions > 0 && ` (${pendingActions} pending)`}
          </span>
        </div>
      )}

      {/* Filters */}
      <TaskFilters
        filters={filters}
        viewMode={viewMode}
        onFiltersChange={setFilters}
        onViewModeChange={setViewMode}
        onResetFilters={resetFilters}
        hasActiveFilters={hasActiveFilters}
        completedToday={completedToday}
        totalToday={totalToday}
      />

      {/* Multi-select toolbar */}
      {isMultiSelectMode && (
        <div className={styles.multiSelectBar}>
          <button className={styles.multiSelectButton} onClick={clearSelection}>
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
            Cancel
          </button>
          <span className={styles.selectionCount}>
            {selectedTaskIds.size} selected
          </span>
          <div className={styles.multiSelectActions}>
            <button className={styles.selectAllButton} onClick={selectAll}>
              Select All
            </button>
            <button
              className={styles.bulkCompleteButton}
              onClick={handleBulkComplete}
              disabled={selectedTaskIds.size === 0}
            >
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
              >
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </button>
            <button
              className={styles.bulkDeleteButton}
              onClick={handleBulkDelete}
              disabled={selectedTaskIds.size === 0}
            >
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
              >
                <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Task list */}
      <TaskList
        groups={groupedTasks}
        isLoading={isLoading}
        error={error}
        onComplete={completeTask}
        onDelete={deleteTask}
        onTaskPress={handleTaskPress}
        onTaskLongPress={handleTaskLongPress}
        selectedTaskIds={selectedTaskIds}
        isMultiSelectMode={isMultiSelectMode}
        onRefresh={refresh}
        isRefreshing={isRefreshing}
        emptyMessage={
          hasActiveFilters
            ? 'No tasks match your filters'
            : viewMode === 'completed'
            ? 'No completed tasks'
            : 'No tasks yet. Tap + to create one.'
        }
      />

      {/* FAB for new task */}
      {!isMultiSelectMode && (
        <button
          className={styles.fab}
          onClick={handleOpenNewForm}
          aria-label="Create new task"
        >
          <svg
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
          >
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </button>
      )}

      {/* Undo toast */}
      {showUndoToast && (
        <div className={styles.undoToast}>
          <span>{undoLabel}</span>
          <button className={styles.undoButton} onClick={handleUndo}>
            Undo
          </button>
        </div>
      )}

      {/* Task detail sheet */}
      <TaskDetailSheet
        task={selectedTask}
        isOpen={selectedTask !== null}
        onClose={handleCloseDetail}
        onComplete={completeTask}
        onUncomplete={uncompleteTask}
        onDelete={deleteTask}
        onEdit={handleEditFromDetail}
      />

      {/* Task form sheet */}
      <TaskFormSheet
        isOpen={showFormSheet}
        onClose={handleCloseForm}
        onSave={handleSaveTask}
        editingTask={editingTask}
      />
    </div>
  )
}

export default MobileTasks
