/**
 * Types for Mobile Task Management
 */

export type TaskPriority = 'high' | 'medium' | 'low'
export type TaskStatus = 'pending' | 'completed'
export type TaskViewMode = 'my' | 'all' | 'completed'
export type TaskSortBy = 'dueDate' | 'priority' | 'createdAt' | 'alphabetical'
export type TaskGroupBy = 'overdue' | 'today' | 'tomorrow' | 'thisWeek' | 'later'

export interface MobileTask {
  id: number
  title: string
  description?: string
  orderId?: number
  orderName?: string
  barrelId?: number
  barrelSku?: string
  assigneeId: number | null
  assigneeName?: string
  assigneeAvatar?: string
  dueDate: string
  dueTime?: string
  priority: TaskPriority
  isComplete: boolean
  createdAt: string
  createdBy?: string
}

export interface TaskGroup {
  key: TaskGroupBy
  label: string
  tasks: MobileTask[]
}

export interface TaskFiltersState {
  status: TaskStatus | 'all'
  priority: TaskPriority | 'all'
  dueDateRange: 'all' | 'overdue' | 'today' | 'thisWeek' | 'thisMonth'
  orderId: number | null
  sortBy: TaskSortBy
  sortOrder: 'asc' | 'desc'
}

export interface UseTasksStateReturn {
  // Tasks
  tasks: MobileTask[]
  groupedTasks: TaskGroup[]
  isLoading: boolean
  error: string | null

  // View mode
  viewMode: TaskViewMode
  setViewMode: (mode: TaskViewMode) => void

  // Filters
  filters: TaskFiltersState
  setFilters: (filters: Partial<TaskFiltersState>) => void
  resetFilters: () => void
  hasActiveFilters: boolean

  // Task counts
  completedToday: number
  totalToday: number

  // Selection
  selectedTaskIds: Set<number>
  isMultiSelectMode: boolean
  toggleMultiSelect: (taskId?: number) => void
  selectAll: () => void
  clearSelection: () => void

  // Actions
  completeTask: (taskId: number) => Promise<void>
  uncompleteTask: (taskId: number) => Promise<void>
  deleteTask: (taskId: number) => Promise<void>
  bulkComplete: (taskIds: number[]) => Promise<void>
  bulkDelete: (taskIds: number[]) => Promise<void>

  // Undo
  undoAction: () => Promise<void>
  canUndo: boolean
  undoLabel: string | null

  // Detail/Edit
  selectedTask: MobileTask | null
  selectTask: (taskId: number | null) => void

  // Refresh
  refresh: () => Promise<void>
  isRefreshing: boolean

  // Offline
  isOffline: boolean
  pendingActions: number
}

export interface CreateTaskData {
  title: string
  description?: string
  orderId?: number
  assigneeId?: number
  dueDate: string
  dueTime?: string
  priority: TaskPriority
}

export interface UpdateTaskData {
  title?: string
  description?: string
  orderId?: number
  assigneeId?: number | null
  dueDate?: string
  dueTime?: string
  priority?: TaskPriority
}

// Swipe gesture constants
export const SWIPE_THRESHOLD = 0.4 // 40% of item width
export const VELOCITY_THRESHOLD = 0.5 // pixels per ms
export const UNDO_TIMEOUT_MS = 5000
