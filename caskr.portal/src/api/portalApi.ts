const API_BASE = '/api/portal'

const STORAGE_KEYS = {
  token: 'portal.token',
  user: 'portal.user',
  expiresAt: 'portal.expiresAt'
}

export const getStoredToken = (): string | null => {
  return localStorage.getItem(STORAGE_KEYS.token)
}

export const setStoredAuth = (token: string, user: object, expiresAt: string) => {
  localStorage.setItem(STORAGE_KEYS.token, token)
  localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(user))
  localStorage.setItem(STORAGE_KEYS.expiresAt, expiresAt)
}

export const clearStoredAuth = () => {
  localStorage.removeItem(STORAGE_KEYS.token)
  localStorage.removeItem(STORAGE_KEYS.user)
  localStorage.removeItem(STORAGE_KEYS.expiresAt)
}

export const getStoredUser = () => {
  const userJson = localStorage.getItem(STORAGE_KEYS.user)
  return userJson ? JSON.parse(userJson) : null
}

export const getStoredExpiresAt = (): string | null => {
  return localStorage.getItem(STORAGE_KEYS.expiresAt)
}

export async function portalFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getStoredToken()

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers
  }

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers
  })

  if (!response.ok) {
    let errorMessage = 'Request failed'
    try {
      const errorData = await response.json()
      errorMessage = errorData.message || errorMessage
    } catch {
      // Use default error message
    }
    throw new Error(errorMessage)
  }

  // Handle empty responses
  const contentType = response.headers.get('content-type')
  if (contentType && contentType.includes('application/json')) {
    return response.json()
  }
  return {} as T
}

// Auth API
export const authApi = {
  register: (data: {
    email: string
    password: string
    firstName: string
    lastName: string
    phone?: string
    companyId: number
  }) =>
    portalFetch<{ message: string; userId: number; email: string }>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  login: (email: string, password: string) =>
    portalFetch<{ accessToken: string; expiresAt: string; user: object }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    }),

  verifyEmail: (token: string) =>
    portalFetch<{ success: boolean; message: string }>(`/auth/verify?token=${token}`),

  forgotPassword: (email: string) =>
    portalFetch<{ message: string }>('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email })
    }),

  resetPassword: (token: string, newPassword: string) =>
    portalFetch<{ message: string }>('/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ token, newPassword })
    }),

  logout: () =>
    portalFetch<{ message: string }>('/auth/logout', {
      method: 'POST'
    })
}

// Barrels API
export const barrelsApi = {
  getMyBarrels: () =>
    portalFetch<Array<{
      id: number
      barrel: object
      purchaseDate: string
      purchasePrice?: number
      ownershipPercentage: number
      certificateNumber?: string
      status: string
      documents: object[]
    }>>('/barrels'),

  getBarrelDetail: (id: number) =>
    portalFetch<{
      id: number
      barrel: object
      purchaseDate: string
      purchasePrice?: number
      ownershipPercentage: number
      certificateNumber?: string
      status: string
      notes?: string
      documents: object[]
    }>(`/barrels/${id}`)
}

// Documents API
export const documentsApi = {
  downloadDocument: async (documentId: number): Promise<Blob> => {
    const token = getStoredToken()
    const response = await fetch(`${API_BASE}/documents/${documentId}/download`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    })
    if (!response.ok) {
      throw new Error('Failed to download document')
    }
    return response.blob()
  }
}

// Notifications API
export const notificationsApi = {
  getNotifications: () =>
    portalFetch<Array<{
      id: number
      notificationType: string
      title: string
      message: string
      isRead: boolean
      sentAt: string
    }>>('/notifications'),

  markAsRead: (id: number) =>
    portalFetch<void>(`/notifications/${id}/read`, { method: 'POST' })
}
