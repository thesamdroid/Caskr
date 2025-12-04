import { getStoredToken } from './portalApi'
import type {
  PricingTier,
  PricingFeature,
  PricingTierFeature,
  PricingFaq,
  PricingPromotion,
  PricingAuditLog,
  PricingPageData
} from '../types/pricing'

const API_BASE = '/api/admin/pricing'

async function adminFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getStoredToken()

  if (!token) {
    throw new Error('Not authenticated')
  }

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
    ...options.headers
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers
  })

  if (response.status === 403) {
    throw new Error('Access denied. Admin privileges required.')
  }

  if (response.status === 401) {
    throw new Error('Session expired. Please log in again.')
  }

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

  // Handle empty responses (204 No Content)
  if (response.status === 204) {
    return {} as T
  }

  const contentType = response.headers.get('content-type')
  if (contentType && contentType.includes('application/json')) {
    return response.json()
  }
  return {} as T
}

// Tiers API
export const tiersApi = {
  getAll: () => adminFetch<PricingTier[]>('/tiers'),

  getById: (id: number) => adminFetch<PricingTier>(`/tiers/${id}`),

  create: (tier: Partial<PricingTier>) =>
    adminFetch<PricingTier>('/tiers', {
      method: 'POST',
      body: JSON.stringify(tier)
    }),

  update: (id: number, tier: Partial<PricingTier>) =>
    adminFetch<PricingTier>(`/tiers/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...tier, id })
    }),

  delete: (id: number) =>
    adminFetch<void>(`/tiers/${id}`, {
      method: 'DELETE'
    }),

  reorder: (tierIds: number[]) =>
    adminFetch<void>('/tiers/reorder', {
      method: 'POST',
      body: JSON.stringify({ tierIds })
    })
}

// Features API
export const featuresApi = {
  getAll: () => adminFetch<PricingFeature[]>('/features'),

  create: (feature: Partial<PricingFeature>) =>
    adminFetch<PricingFeature>('/features', {
      method: 'POST',
      body: JSON.stringify(feature)
    }),

  update: (id: number, feature: Partial<PricingFeature>) =>
    adminFetch<PricingFeature>(`/features/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...feature, id })
    }),

  delete: (id: number) =>
    adminFetch<void>(`/features/${id}`, {
      method: 'DELETE'
    })
}

// Tier Features API (junction)
export const tierFeaturesApi = {
  addToTier: (tierId: number, tierFeature: Partial<PricingTierFeature>) =>
    adminFetch<PricingTierFeature>(`/tiers/${tierId}/features`, {
      method: 'POST',
      body: JSON.stringify(tierFeature)
    }),

  updateForTier: (tierId: number, featureId: number, tierFeature: Partial<PricingTierFeature>) =>
    adminFetch<PricingTierFeature>(`/tiers/${tierId}/features/${featureId}`, {
      method: 'PUT',
      body: JSON.stringify(tierFeature)
    }),

  removeFromTier: (tierId: number, featureId: number) =>
    adminFetch<void>(`/tiers/${tierId}/features/${featureId}`, {
      method: 'DELETE'
    })
}

// FAQs API
export const faqsApi = {
  getAll: () => adminFetch<PricingFaq[]>('/faqs'),

  create: (faq: Partial<PricingFaq>) =>
    adminFetch<PricingFaq>('/faqs', {
      method: 'POST',
      body: JSON.stringify(faq)
    }),

  update: (id: number, faq: Partial<PricingFaq>) =>
    adminFetch<PricingFaq>(`/faqs/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...faq, id })
    }),

  delete: (id: number) =>
    adminFetch<void>(`/faqs/${id}`, {
      method: 'DELETE'
    }),

  reorder: (faqIds: number[]) =>
    adminFetch<void>('/faqs/reorder', {
      method: 'POST',
      body: JSON.stringify({ faqIds })
    })
}

// Promotions API
export const promotionsApi = {
  getAll: () => adminFetch<PricingPromotion[]>('/promotions'),

  create: (promo: Partial<PricingPromotion>) =>
    adminFetch<PricingPromotion>('/promotions', {
      method: 'POST',
      body: JSON.stringify(promo)
    }),

  update: (id: number, promo: Partial<PricingPromotion>) =>
    adminFetch<PricingPromotion>(`/promotions/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...promo, id })
    }),

  delete: (id: number) =>
    adminFetch<void>(`/promotions/${id}`, {
      method: 'DELETE'
    }),

  generateCode: (prefix?: string) =>
    adminFetch<{ code: string }>('/promotions/generate-code', {
      method: 'POST',
      body: JSON.stringify({ prefix })
    }),

  importCsv: (csvData: string) =>
    adminFetch<{ imported: number; errors: string[] }>('/promotions/import', {
      method: 'POST',
      body: JSON.stringify({ csvData })
    }),

  exportCsv: () => adminFetch<{ csv: string }>('/promotions/export')
}

// Audit Logs API
export const auditLogsApi = {
  getAll: (params?: {
    entityType?: string
    entityId?: number
    startDate?: string
    endDate?: string
    userId?: number
    limit?: number
  }) => {
    const searchParams = new URLSearchParams()
    if (params?.entityType) searchParams.set('entityType', params.entityType)
    if (params?.entityId) searchParams.set('entityId', params.entityId.toString())
    if (params?.startDate) searchParams.set('startDate', params.startDate)
    if (params?.endDate) searchParams.set('endDate', params.endDate)
    if (params?.userId) searchParams.set('userId', params.userId.toString())
    if (params?.limit) searchParams.set('limit', params.limit.toString())

    const query = searchParams.toString()
    return adminFetch<PricingAuditLog[]>(`/audit-logs${query ? `?${query}` : ''}`)
  },

  exportCsv: (params?: {
    startDate?: string
    endDate?: string
  }) => {
    const searchParams = new URLSearchParams()
    if (params?.startDate) searchParams.set('startDate', params.startDate)
    if (params?.endDate) searchParams.set('endDate', params.endDate)

    const query = searchParams.toString()
    return adminFetch<{ csv: string }>(`/audit-logs/export${query ? `?${query}` : ''}`)
  }
}

// Preview API
export const previewApi = {
  getPreview: () => adminFetch<PricingPageData>('/preview'),

  publishChanges: () =>
    adminFetch<{ message: string }>('/publish', {
      method: 'POST'
    })
}

// Check if current user has admin access
export const checkAdminAccess = async (): Promise<boolean> => {
  try {
    await adminFetch<PricingTier[]>('/tiers')
    return true
  } catch {
    return false
  }
}
