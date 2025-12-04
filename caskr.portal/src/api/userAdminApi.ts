// User Admin API
import type {
  UserListResponse,
  UserSearchFilters,
  UserDetail,
  UserAuditLog,
  UserSession,
  UserNote,
  UserEditFormData,
  SuspendUserFormData,
  DeleteUserFormData,
  ImpersonateUserFormData,
  UserNoteFormData,
  SuperAdminAccessCheck,
  ImpersonationSession
} from '../types/userAdmin'

const API_BASE = '/api/admin/users'

async function superAdminFetch<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const token = localStorage.getItem('authToken')
  const superAdminToken = sessionStorage.getItem('superAdminReAuthToken')

  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...(superAdminToken && { 'X-SuperAdmin-ReAuth': superAdminToken }),
      ...options.headers
    }
  })

  if (response.status === 401) {
    throw new Error('Unauthorized')
  }

  if (response.status === 403) {
    const errorData = await response.json().catch(() => ({}))
    if (errorData.requiresReAuth) {
      throw new Error('RE_AUTH_REQUIRED')
    }
    throw new Error('Access denied')
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    throw new Error(errorData.message || 'API request failed')
  }

  return response.json()
}

// Access control
export const accessApi = {
  checkAccess: (): Promise<SuperAdminAccessCheck> =>
    superAdminFetch(`${API_BASE}/access-check`),

  reAuthenticate: (password: string): Promise<{ token: string; expiresAt: string }> =>
    superAdminFetch(`${API_BASE}/re-authenticate`, {
      method: 'POST',
      body: JSON.stringify({ password })
    }),

  endSession: (): Promise<void> =>
    superAdminFetch(`${API_BASE}/end-session`, { method: 'POST' })
}

// User listing and search
export const usersApi = {
  list: (filters: Partial<UserSearchFilters>): Promise<UserListResponse> => {
    const params = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== '') {
        params.append(key, String(value))
      }
    })
    return superAdminFetch(`${API_BASE}?${params.toString()}`)
  },

  getById: (userId: string): Promise<UserDetail> =>
    superAdminFetch(`${API_BASE}/${userId}`),

  update: (userId: string, data: UserEditFormData): Promise<UserDetail> =>
    superAdminFetch(`${API_BASE}/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    }),

  resetPassword: (userId: string): Promise<{ tempPassword?: string }> =>
    superAdminFetch(`${API_BASE}/${userId}/reset-password`, { method: 'POST' }),

  forceLogout: (userId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/force-logout`, { method: 'POST' }),

  unlockAccount: (userId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/unlock`, { method: 'POST' }),

  disable2FA: (userId: string, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/disable-2fa`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    }),

  resendVerification: (userId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/resend-verification`, { method: 'POST' }),

  verifyEmail: (userId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/verify-email`, { method: 'POST' })
}

// Account operations
export const accountOpsApi = {
  suspend: (userId: string, data: SuspendUserFormData): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/suspend`, {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  unsuspend: (userId: string, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/unsuspend`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    }),

  delete: (userId: string, data: DeleteUserFormData): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/delete`, {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  restore: (userId: string, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/restore`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    })
}

// Impersonation
export const impersonationApi = {
  start: (userId: string, data: ImpersonateUserFormData): Promise<ImpersonationSession> =>
    superAdminFetch(`${API_BASE}/${userId}/impersonate`, {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  end: (): Promise<void> =>
    superAdminFetch(`${API_BASE}/impersonate/end`, { method: 'POST' }),

  getCurrentSession: (): Promise<ImpersonationSession | null> =>
    superAdminFetch(`${API_BASE}/impersonate/current`)
}

// User sessions
export const sessionsApi = {
  list: (userId: string): Promise<UserSession[]> =>
    superAdminFetch(`${API_BASE}/${userId}/sessions`),

  terminate: (userId: string, sessionId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/sessions/${sessionId}`, { method: 'DELETE' }),

  terminateAll: (userId: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/sessions`, { method: 'DELETE' })
}

// Audit logs
export const userAuditApi = {
  list: (userId: string, limit?: number): Promise<UserAuditLog[]> => {
    const params = limit ? `?limit=${limit}` : ''
    return superAdminFetch(`${API_BASE}/${userId}/audit-log${params}`)
  },

  export: (userId: string): Promise<{ csv: string }> =>
    superAdminFetch(`${API_BASE}/${userId}/audit-log/export`)
}

// Admin notes
export const notesApi = {
  list: (userId: string): Promise<UserNote[]> =>
    superAdminFetch(`${API_BASE}/${userId}/notes`),

  create: (userId: string, data: UserNoteFormData): Promise<UserNote> =>
    superAdminFetch(`${API_BASE}/${userId}/notes`, {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  update: (userId: string, noteId: number, data: UserNoteFormData): Promise<UserNote> =>
    superAdminFetch(`${API_BASE}/${userId}/notes/${noteId}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    }),

  delete: (userId: string, noteId: number): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/notes/${noteId}`, { method: 'DELETE' })
}

// Subscription management
export const subscriptionApi = {
  getDetails: (userId: string): Promise<{
    subscriptionId: string | null
    status: string
    tier: string | null
    startDate: string | null
    endDate: string | null
    cancelAtPeriodEnd: boolean
    invoices: Array<{
      id: string
      date: string
      amount: number
      status: string
    }>
  }> =>
    superAdminFetch(`${API_BASE}/${userId}/subscription`),

  changeTier: (userId: string, tierId: number, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/subscription/change-tier`, {
      method: 'POST',
      body: JSON.stringify({ tierId, reason })
    }),

  cancel: (userId: string, reason: string, immediate: boolean): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/subscription/cancel`, {
      method: 'POST',
      body: JSON.stringify({ reason, immediate })
    }),

  extend: (userId: string, days: number, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/subscription/extend`, {
      method: 'POST',
      body: JSON.stringify({ days, reason })
    }),

  grantFree: (userId: string, tierId: number, durationMonths: number, reason: string): Promise<void> =>
    superAdminFetch(`${API_BASE}/${userId}/subscription/grant-free`, {
      method: 'POST',
      body: JSON.stringify({ tierId, durationMonths, reason })
    })
}

// Export data
export const exportApi = {
  usersList: (filters: Partial<UserSearchFilters>): Promise<{ csv: string }> => {
    const params = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== '') {
        params.append(key, String(value))
      }
    })
    return superAdminFetch(`${API_BASE}/export?${params.toString()}`)
  }
}
