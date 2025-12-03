import { useState, useCallback, useEffect, useRef, useMemo } from 'react'
import { authorizedFetch } from '../../../api/authorizedFetch'
import { useAppSelector } from '../../../hooks'
import type {
  MobileTask,
  TaskGroup,
  TaskFiltersState,
  TaskViewMode,
  TaskGroupBy,
  UseTasksStateReturn,
} from './types'

const CACHE_KEY = 'caskr_mobile_tasks_cache'

interface CachedTasks {
  tasks: MobileTask[]
  timestamp: number
}

interface UndoState {
  type: 'complete' | 'uncomplete' | 'delete' | 'bulkComplete' | 'bulkDelete'
  taskIds: number[]
  originalTasks: MobileTask[]
  label: string
}

const defaultFilters: TaskFiltersState = {
  status: 'all',
  priority: 'all',
  dueDateRange: 'all',
  orderId: null,
  sortBy: 'dueDate',
  sortOrder: 'asc'
}

/**
 * Load cached tasks
 */
function loadCachedTasks(): CachedTasks | null {
  try {
    const cached = localStorage.getItem(CACHE_KEY)
    if (!cached) return null
    return JSON.parse(cached) as CachedTasks
  } catch {
    return null
  }
}

/**
 * Save tasks to cache
 */
function saveCachedTasks(tasks: MobileTask[]): void {
  try {
    const cache: CachedTasks = { tasks, timestamp: Date.now() }
    localStorage.setItem(CACHE_KEY, JSON.stringify(cache))
  } catch {
    // Ignore storage errors
  }
}

/**
 * Get task group based on due date
 */
function getTaskGroup(dueDate: string): TaskGroupBy {
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const due = new Date(dueDate)
  due.setHours(0, 0, 0, 0)

  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  const weekEnd = new Date(today)
  weekEnd.setDate(weekEnd.getDate() + 7)

  if (due < today) return 'overdue'
  if (due.getTime() === today.getTime()) return 'today'
  if (due.getTime() === tomorrow.getTime()) return 'tomorrow'
  if (due < weekEnd) return 'thisWeek'
  return 'later'
}

/**
 * Group label for task groups
 */
const groupLabels: Record<TaskGroupBy, string> = {
  overdue: 'Overdue',
  today: 'Today',
  tomorrow: 'Tomorrow',
  thisWeek: 'This Week',
  later: 'Later'
}

/**
 * Hook for managing mobile task state
 */
export function useTasksState(): UseTasksStateReturn {
  const user = useAppSelector(state => state.auth.user)

  // Tasks state
  const [tasks, setTasks] = useState<MobileTask[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // View mode
  const [viewMode, setViewMode] = useState<TaskViewMode>('my')

  // Filters
  const [filters, setFiltersState] = useState<TaskFiltersState>(defaultFilters)

  // Selection
  const [selectedTaskIds, setSelectedTaskIds] = useState<Set<number>>(new Set())
  const [isMultiSelectMode, setIsMultiSelectMode] = useState(false)

  // Selected task for detail view
  const [selectedTask, setSelectedTask] = useState<MobileTask | null>(null)

  // Undo state
  const [undoState, setUndoState] = useState<UndoState | null>(null)
  const undoTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Offline state
  const [isOffline, setIsOffline] = useState(!navigator.onLine)
  const [pendingActions] = useState(0) // Future: sync pending actions with IndexedDB

  // Handle online/offline
  useEffect(() => {
    const handleOnline = () => setIsOffline(false)
    const handleOffline = () => setIsOffline(true)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  // Filter tasks based on view mode and filters
  const filteredTasks = useMemo(() => {
    let result = [...tasks]

    // Filter by view mode
    if (viewMode === 'my' && user) {
      result = result.filter(t => t.assigneeId === user.id)
    } else if (viewMode === 'completed') {
      result = result.filter(t => t.isComplete)
    } else {
      result = result.filter(t => !t.isComplete)
    }

    // Apply filters
    if (filters.status !== 'all') {
      result = result.filter(t =>
        filters.status === 'completed' ? t.isComplete : !t.isComplete
      )
    }

    if (filters.priority !== 'all') {
      result = result.filter(t => t.priority === filters.priority)
    }

    if (filters.orderId !== null) {
      result = result.filter(t => t.orderId === filters.orderId)
    }

    if (filters.dueDateRange !== 'all') {
      const today = new Date()
      today.setHours(0, 0, 0, 0)

      result = result.filter(t => {
        const due = new Date(t.dueDate)
        due.setHours(0, 0, 0, 0)

        switch (filters.dueDateRange) {
          case 'overdue':
            return due < today && !t.isComplete
          case 'today':
            return due.getTime() === today.getTime()
          case 'thisWeek': {
            const weekEnd = new Date(today)
            weekEnd.setDate(weekEnd.getDate() + 7)
            return due >= today && due < weekEnd
          }
          case 'thisMonth': {
            const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0)
            return due >= today && due <= monthEnd
          }
          default:
            return true
        }
      })
    }

    // Sort
    result.sort((a, b) => {
      let comparison = 0

      switch (filters.sortBy) {
        case 'dueDate':
          comparison = new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
          break
        case 'priority': {
          const priorityOrder: Record<string, number> = { high: 0, medium: 1, low: 2 }
          comparison = priorityOrder[a.priority] - priorityOrder[b.priority]
          break
        }
        case 'createdAt':
          comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
          break
        case 'alphabetical':
          comparison = a.title.localeCompare(b.title)
          break
      }

      return filters.sortOrder === 'asc' ? comparison : -comparison
    })

    return result
  }, [tasks, viewMode, filters, user])

  // Group tasks by due date
  const groupedTasks = useMemo<TaskGroup[]>(() => {
    const groups: Map<TaskGroupBy, MobileTask[]> = new Map()

    // Initialize groups in order
    const groupOrder: TaskGroupBy[] = ['overdue', 'today', 'tomorrow', 'thisWeek', 'later']
    groupOrder.forEach(key => groups.set(key, []))

    // Group tasks
    filteredTasks.forEach(task => {
      const group = getTaskGroup(task.dueDate)
      groups.get(group)?.push(task)
    })

    // Convert to array, filter empty groups
    return groupOrder
      .map(key => ({
        key,
        label: groupLabels[key],
        tasks: groups.get(key) || []
      }))
      .filter(g => g.tasks.length > 0)
  }, [filteredTasks])

  // Task counts
  const completedToday = useMemo(() => {
    const today = new Date().toISOString().split('T')[0]
    return tasks.filter(t => t.isComplete && t.dueDate.startsWith(today)).length
  }, [tasks])

  const totalToday = useMemo(() => {
    const today = new Date().toISOString().split('T')[0]
    return tasks.filter(t => t.dueDate.startsWith(today)).length
  }, [tasks])

  // Check if filters are active
  const hasActiveFilters = useMemo(() => {
    return filters.status !== 'all' ||
      filters.priority !== 'all' ||
      filters.dueDateRange !== 'all' ||
      filters.orderId !== null ||
      filters.sortBy !== 'dueDate'
  }, [filters])

  // Fetch tasks
  /**
   * Format error message based on response status or error type
   */
  const formatTaskError = (status?: number, err?: unknown): string => {
    if (status) {
      if (status >= 500) return 'Unable to load tasks. Our servers are experiencing issues.'
      if (status === 401 || status === 403) return 'Please sign in to view your tasks.'
      if (status === 429) return 'Too many requests. Please wait a moment.'
    }
    if (err instanceof TypeError && err.message.includes('fetch')) {
      return 'Network error. Please check your connection.'
    }
    return 'Unable to load tasks. Pull down to try again.'
  }

  const fetchTasks = useCallback(async (isBackground = false) => {
    if (!isBackground) {
      setIsLoading(true)
    }
    setError(null)

    try {
      const response = await authorizedFetch('api/tasks')
      if (!response.ok) {
        throw { status: response.status, message: formatTaskError(response.status) }
      }

      const data = await response.json() as MobileTask[]
      setTasks(data)
      saveCachedTasks(data)
    } catch (err) {
      console.error('[useTasksState] Fetch error:', err)

      const errorMessage = (err as { message?: string })?.message || formatTaskError(undefined, err)
      setError(errorMessage)

      // Try to load from cache
      const cached = loadCachedTasks()
      if (cached) {
        setTasks(cached.tasks)
        // Update error to indicate we're showing cached data
        if (!isBackground) {
          setError('Showing cached tasks. Pull to refresh.')
        }
      }
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }, [])

  // Initial load
  useEffect(() => {
    // Load from cache first
    const cached = loadCachedTasks()
    if (cached) {
      setTasks(cached.tasks)
      setIsLoading(false)
    }

    // Then fetch fresh data
    if (!isOffline) {
      fetchTasks(!cached)
    }
  }, [fetchTasks, isOffline])

  // Refresh
  const refresh = useCallback(async () => {
    setIsRefreshing(true)
    await fetchTasks(true)
  }, [fetchTasks])

  // Set filters
  const setFilters = useCallback((newFilters: Partial<TaskFiltersState>) => {
    setFiltersState(prev => ({ ...prev, ...newFilters }))
  }, [])

  // Reset filters
  const resetFilters = useCallback(() => {
    setFiltersState(defaultFilters)
  }, [])

  // Toggle multi-select mode
  const toggleMultiSelect = useCallback((taskId?: number) => {
    if (taskId !== undefined) {
      if (!isMultiSelectMode) {
        setIsMultiSelectMode(true)
        setSelectedTaskIds(new Set([taskId]))
      } else {
        setSelectedTaskIds(prev => {
          const next = new Set(prev)
          if (next.has(taskId)) {
            next.delete(taskId)
          } else {
            next.add(taskId)
          }
          return next
        })
      }
    } else {
      setIsMultiSelectMode(false)
      setSelectedTaskIds(new Set())
    }
  }, [isMultiSelectMode])

  // Select all visible tasks
  const selectAll = useCallback(() => {
    const ids = new Set(filteredTasks.map(t => t.id))
    setSelectedTaskIds(ids)
  }, [filteredTasks])

  // Clear selection
  const clearSelection = useCallback(() => {
    setSelectedTaskIds(new Set())
    setIsMultiSelectMode(false)
  }, [])

  // Setup undo with timeout
  const setupUndo = useCallback((state: UndoState) => {
    if (undoTimeoutRef.current) {
      clearTimeout(undoTimeoutRef.current)
    }

    setUndoState(state)

    undoTimeoutRef.current = setTimeout(() => {
      setUndoState(null)
    }, 5000) // UNDO_TIMEOUT_MS
  }, [])

  // Complete task
  const completeTask = useCallback(async (taskId: number) => {
    const task = tasks.find(t => t.id === taskId)
    if (!task) return

    // Save for undo
    setupUndo({
      type: 'complete',
      taskIds: [taskId],
      originalTasks: [task],
      label: 'Task completed'
    })

    // Optimistic update
    setTasks(prev => prev.map(t =>
      t.id === taskId ? { ...t, isComplete: true } : t
    ))

    try {
      const response = await authorizedFetch(`api/tasks/${taskId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isComplete: true })
      })

      if (!response.ok) {
        throw new Error('Failed to complete task')
      }
    } catch (err) {
      // Rollback
      setTasks(prev => prev.map(t =>
        t.id === taskId ? { ...t, isComplete: false } : t
      ))
      setUndoState(null)
      throw err
    }
  }, [tasks, setupUndo])

  // Uncomplete task
  const uncompleteTask = useCallback(async (taskId: number) => {
    const task = tasks.find(t => t.id === taskId)
    if (!task) return

    // Optimistic update
    setTasks(prev => prev.map(t =>
      t.id === taskId ? { ...t, isComplete: false } : t
    ))

    try {
      const response = await authorizedFetch(`api/tasks/${taskId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isComplete: false })
      })

      if (!response.ok) {
        throw new Error('Failed to uncomplete task')
      }
    } catch (err) {
      // Rollback
      setTasks(prev => prev.map(t =>
        t.id === taskId ? { ...t, isComplete: true } : t
      ))
      throw err
    }
  }, [tasks])

  // Delete task
  const deleteTask = useCallback(async (taskId: number) => {
    const task = tasks.find(t => t.id === taskId)
    if (!task) return

    // Save for undo
    setupUndo({
      type: 'delete',
      taskIds: [taskId],
      originalTasks: [task],
      label: 'Task deleted'
    })

    // Optimistic update
    setTasks(prev => prev.filter(t => t.id !== taskId))

    try {
      const response = await authorizedFetch(`api/tasks/${taskId}`, {
        method: 'DELETE'
      })

      if (!response.ok) {
        throw new Error('Failed to delete task')
      }
    } catch (err) {
      // Rollback
      setTasks(prev => [...prev, task])
      setUndoState(null)
      throw err
    }
  }, [tasks, setupUndo])

  // Bulk complete
  const bulkComplete = useCallback(async (taskIds: number[]) => {
    const tasksToComplete = tasks.filter(t => taskIds.includes(t.id))
    if (tasksToComplete.length === 0) return

    // Save for undo
    setupUndo({
      type: 'bulkComplete',
      taskIds,
      originalTasks: tasksToComplete,
      label: `${taskIds.length} tasks completed`
    })

    // Optimistic update
    setTasks(prev => prev.map(t =>
      taskIds.includes(t.id) ? { ...t, isComplete: true } : t
    ))
    clearSelection()

    try {
      await Promise.all(taskIds.map(id =>
        authorizedFetch(`api/tasks/${id}/complete`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ isComplete: true })
        })
      ))
    } catch (err) {
      // Rollback
      setTasks(prev => prev.map(t => {
        const original = tasksToComplete.find(o => o.id === t.id)
        return original ? original : t
      }))
      setUndoState(null)
      throw err
    }
  }, [tasks, setupUndo, clearSelection])

  // Bulk delete
  const bulkDelete = useCallback(async (taskIds: number[]) => {
    const tasksToDelete = tasks.filter(t => taskIds.includes(t.id))
    if (tasksToDelete.length === 0) return

    // Save for undo
    setupUndo({
      type: 'bulkDelete',
      taskIds,
      originalTasks: tasksToDelete,
      label: `${taskIds.length} tasks deleted`
    })

    // Optimistic update
    setTasks(prev => prev.filter(t => !taskIds.includes(t.id)))
    clearSelection()

    try {
      await Promise.all(taskIds.map(id =>
        authorizedFetch(`api/tasks/${id}`, { method: 'DELETE' })
      ))
    } catch (err) {
      // Rollback
      setTasks(prev => [...prev, ...tasksToDelete])
      setUndoState(null)
      throw err
    }
  }, [tasks, setupUndo, clearSelection])

  // Undo action
  const undoAction = useCallback(async () => {
    if (!undoState) return

    if (undoTimeoutRef.current) {
      clearTimeout(undoTimeoutRef.current)
    }

    const { type, taskIds, originalTasks } = undoState
    setUndoState(null)

    try {
      switch (type) {
        case 'complete':
          await uncompleteTask(taskIds[0])
          break
        case 'uncomplete':
          await completeTask(taskIds[0])
          break
        case 'delete':
        case 'bulkDelete':
          // Re-add tasks
          setTasks(prev => [...prev, ...originalTasks])
          // Re-create on server
          // Note: This is a simplified version - in production you'd need a restore endpoint
          break
        case 'bulkComplete':
          // Uncomplete all
          setTasks(prev => prev.map(t => {
            const original = originalTasks.find(o => o.id === t.id)
            return original ? original : t
          }))
          break
      }
    } catch (err) {
      console.error('[useTasksState] Undo error:', err)
    }
  }, [undoState, uncompleteTask, completeTask])

  // Select task for detail view
  const selectTask = useCallback((taskId: number | null) => {
    if (taskId === null) {
      setSelectedTask(null)
    } else {
      const task = tasks.find(t => t.id === taskId)
      setSelectedTask(task || null)
    }
  }, [tasks])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (undoTimeoutRef.current) {
        clearTimeout(undoTimeoutRef.current)
      }
    }
  }, [])

  return {
    // Tasks
    tasks: filteredTasks,
    groupedTasks,
    isLoading,
    error,

    // View mode
    viewMode,
    setViewMode,

    // Filters
    filters,
    setFilters,
    resetFilters,
    hasActiveFilters,

    // Task counts
    completedToday,
    totalToday,

    // Selection
    selectedTaskIds,
    isMultiSelectMode,
    toggleMultiSelect,
    selectAll,
    clearSelection,

    // Actions
    completeTask,
    uncompleteTask,
    deleteTask,
    bulkComplete,
    bulkDelete,

    // Undo
    undoAction,
    canUndo: undoState !== null,
    undoLabel: undoState?.label || null,

    // Detail/Edit
    selectedTask,
    selectTask,

    // Refresh
    refresh,
    isRefreshing,

    // Offline
    isOffline,
    pendingActions
  }
}

export default useTasksState
