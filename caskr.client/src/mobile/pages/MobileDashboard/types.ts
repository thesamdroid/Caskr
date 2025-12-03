/**
 * Types for the Mobile Dashboard
 */

export type AlertType = 'critical' | 'warning' | 'info'

export interface DashboardAlert {
  id: string
  type: AlertType
  title: string
  message: string
  actionUrl?: string
  dismissible: boolean
}

export interface DashboardTask {
  id: number
  title: string
  description?: string
  assigneeId: number | null
  assigneeName?: string
  dueDate: string
  dueTime?: string
  priority: 'high' | 'medium' | 'low'
  orderId?: number
  orderName?: string
  isComplete: boolean
}

export interface DashboardOrder {
  id: number
  name: string
  progress: number
  barrelCount: number
  totalBarrels: number
  status: string
  dueDate?: string
}

export interface DashboardActivity {
  id: string
  type: 'barrel_filled' | 'task_completed' | 'movement' | 'gauge_recorded' | 'order_created'
  title: string
  description?: string
  timestamp: string
  iconType: 'barrel' | 'task' | 'movement' | 'gauge' | 'order'
}

export interface DashboardData {
  greeting: string
  userName: string
  currentDate: string
  alerts: DashboardAlert[]
  todaysTasks: DashboardTask[]
  activeOrders: DashboardOrder[]
  recentActivity: DashboardActivity[]
  stats: {
    tasksCompletedToday: number
    tasksDueToday: number
    activeBarrels: number
    pendingMovements: number
  }
}

export interface UseDashboardDataReturn {
  data: DashboardData | null
  isLoading: boolean
  isRefreshing: boolean
  error: string | null
  isOffline: boolean
  lastUpdated: Date | null
  refresh: () => Promise<void>
  dismissAlert: (alertId: string) => void
  completeTask: (taskId: number) => Promise<void>
  undoCompleteTask: (taskId: number) => Promise<void>
}

export type QuickActionType = 'scan' | 'new-task' | 'movement' | 'gauge'

export interface QuickAction {
  id: QuickActionType
  label: string
  icon: React.ReactNode
  route: string
}
