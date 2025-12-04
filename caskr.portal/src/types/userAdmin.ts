// User Admin Types for Super-User Portal

export type UserRole = 'User' | 'Admin' | 'SuperAdmin' | 'PricingManager'

export type UserStatus = 'Active' | 'Suspended' | 'PendingVerification' | 'Deleted'

export type SubscriptionStatus = 'Active' | 'Cancelled' | 'PastDue' | 'Trialing' | 'None'

export interface UserListItem {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  status: UserStatus
  subscriptionStatus: SubscriptionStatus
  subscriptionTier: string | null
  createdAt: string
  lastLoginAt: string | null
  distilleryName: string | null
}

export interface UserDetail {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  status: UserStatus
  createdAt: string
  lastLoginAt: string | null
  emailVerifiedAt: string | null

  // Profile
  phone: string | null
  timezone: string | null
  avatarUrl: string | null

  // Distillery
  distilleryId: string | null
  distilleryName: string | null
  distilleryAddress: string | null
  distilleryCity: string | null
  distilleryState: string | null
  distilleryPostalCode: string | null
  dspNumber: string | null
  ttbPermitId: string | null

  // Subscription
  subscriptionId: string | null
  subscriptionStatus: SubscriptionStatus
  subscriptionTier: string | null
  subscriptionStartDate: string | null
  subscriptionEndDate: string | null
  billingEmail: string | null

  // Stats
  totalBarrels: number
  totalBatches: number
  totalLogins: number
  storageUsedMb: number
  teamMemberCount: number

  // Security
  twoFactorEnabled: boolean
  lastPasswordChangeAt: string | null
  failedLoginAttempts: number
  lockedUntil: string | null
}

export interface UserAuditLog {
  id: number
  userId: string
  action: string
  performedBy: string
  performedByName: string
  details: string | null
  ipAddress: string | null
  userAgent: string | null
  createdAt: string
}

export interface UserSession {
  id: string
  userId: string
  ipAddress: string
  userAgent: string
  createdAt: string
  lastActivityAt: string
  expiresAt: string
  isCurrent: boolean
}

export interface UserNote {
  id: number
  userId: string
  content: string
  createdBy: string
  createdByName: string
  createdAt: string
  isImportant: boolean
}

export interface ImpersonationSession {
  originalUserId: string
  originalUserEmail: string
  targetUserId: string
  targetUserEmail: string
  startedAt: string
  reason: string
  expiresAt: string
}

// Form data types
export interface UserSearchFilters {
  search: string
  role: UserRole | ''
  status: UserStatus | ''
  subscriptionStatus: SubscriptionStatus | ''
  subscriptionTier: string
  createdAfter: string
  createdBefore: string
  sortBy: 'email' | 'createdAt' | 'lastLoginAt' | 'distilleryName'
  sortOrder: 'asc' | 'desc'
  page: number
  pageSize: number
}

export interface UserEditFormData {
  firstName: string
  lastName: string
  email: string
  phone: string
  role: UserRole
  timezone: string
}

export interface SuspendUserFormData {
  reason: string
  duration: 'indefinite' | '24h' | '7d' | '30d' | 'custom'
  customDays?: number
  notifyUser: boolean
}

export interface DeleteUserFormData {
  reason: string
  deleteData: boolean
  confirmEmail: string
}

export interface ImpersonateUserFormData {
  reason: string
  duration: number // minutes
  acknowledgement: boolean
}

export interface UserNoteFormData {
  content: string
  isImportant: boolean
}

export interface ReAuthFormData {
  password: string
}

// API response types
export interface UserListResponse {
  users: UserListItem[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface SuperAdminAccessCheck {
  allowed: boolean
  requiresReAuth: boolean
  ipAllowed: boolean
  lastReAuthAt: string | null
}

// Default values
export const defaultUserSearchFilters: UserSearchFilters = {
  search: '',
  role: '',
  status: '',
  subscriptionStatus: '',
  subscriptionTier: '',
  createdAfter: '',
  createdBefore: '',
  sortBy: 'createdAt',
  sortOrder: 'desc',
  page: 1,
  pageSize: 25
}

export const defaultSuspendFormData: SuspendUserFormData = {
  reason: '',
  duration: 'indefinite',
  notifyUser: true
}

export const defaultDeleteFormData: DeleteUserFormData = {
  reason: '',
  deleteData: false,
  confirmEmail: ''
}

export const defaultImpersonateFormData: ImpersonateUserFormData = {
  reason: '',
  duration: 30,
  acknowledgement: false
}

// Helper functions
export function formatUserRole(role: UserRole): string {
  switch (role) {
    case 'SuperAdmin': return 'Super Admin'
    case 'PricingManager': return 'Pricing Manager'
    default: return role
  }
}

export function formatUserStatus(status: UserStatus): string {
  switch (status) {
    case 'PendingVerification': return 'Pending Verification'
    default: return status
  }
}

export function getStatusBadgeClass(status: UserStatus): string {
  switch (status) {
    case 'Active': return 'badge-success'
    case 'Suspended': return 'badge-danger'
    case 'PendingVerification': return 'badge-warning'
    case 'Deleted': return 'badge-secondary'
    default: return 'badge-secondary'
  }
}

export function getRoleBadgeClass(role: UserRole): string {
  switch (role) {
    case 'SuperAdmin': return 'badge-danger'
    case 'Admin': return 'badge-warning'
    case 'PricingManager': return 'badge-info'
    default: return 'badge-secondary'
  }
}

export function getSubscriptionBadgeClass(status: SubscriptionStatus): string {
  switch (status) {
    case 'Active': return 'badge-success'
    case 'Trialing': return 'badge-info'
    case 'PastDue': return 'badge-warning'
    case 'Cancelled': return 'badge-danger'
    case 'None': return 'badge-secondary'
    default: return 'badge-secondary'
  }
}

export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`
}

export function formatRelativeTime(dateString: string | null): string {
  if (!dateString) return 'Never'

  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString()
}
