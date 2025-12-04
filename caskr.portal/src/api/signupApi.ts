import type { SignupFormData, TeamInvite } from '../types/signup'

const API_BASE = '/api'

async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers
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

  const contentType = response.headers.get('content-type')
  if (contentType && contentType.includes('application/json')) {
    return response.json()
  }
  return {} as T
}

// Check if email is available
export const checkEmailAvailability = async (email: string): Promise<{ available: boolean }> => {
  return apiFetch('/signup/check-email', {
    method: 'POST',
    body: JSON.stringify({ email })
  })
}

// Register new user
export const registerUser = async (data: SignupFormData): Promise<{
  userId: number
  email: string
  verificationRequired: boolean
}> => {
  return apiFetch('/signup/register', {
    method: 'POST',
    body: JSON.stringify({
      email: data.email,
      password: data.password,
      firstName: data.firstName,
      lastName: data.lastName,
      distilleryName: data.distilleryName
    })
  })
}

// Send verification code
export const sendVerificationCode = async (email: string): Promise<{ sent: boolean; expiresIn: number }> => {
  return apiFetch('/signup/send-verification', {
    method: 'POST',
    body: JSON.stringify({ email })
  })
}

// Verify code
export const verifyCode = async (
  email: string,
  code: string
): Promise<{ verified: boolean; token?: string }> => {
  return apiFetch('/signup/verify-code', {
    method: 'POST',
    body: JSON.stringify({ email, code })
  })
}

// Verify via magic link
export const verifyMagicLink = async (token: string): Promise<{ verified: boolean }> => {
  return apiFetch(`/signup/verify-magic?token=${token}`)
}

// Create subscription
export const createSubscription = async (data: {
  userId: number
  tierId: number
  billingCycle: 'monthly' | 'annual'
  promoCode?: string
  paymentMethodId: string
  billingAddress: {
    line1: string
    line2?: string
    city: string
    state: string
    postalCode: string
    country: string
  }
}): Promise<{
  subscriptionId: string
  clientSecret?: string
  requiresAction: boolean
}> => {
  return apiFetch('/subscriptions/create', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// Confirm subscription (for 3D Secure)
export const confirmSubscription = async (
  subscriptionId: string,
  paymentIntentId: string
): Promise<{ confirmed: boolean }> => {
  return apiFetch('/subscriptions/confirm', {
    method: 'POST',
    body: JSON.stringify({ subscriptionId, paymentIntentId })
  })
}

// Complete account setup
export const completeAccountSetup = async (userId: number): Promise<{
  accessToken: string
  expiresAt: string
  user: object
}> => {
  return apiFetch('/signup/complete', {
    method: 'POST',
    body: JSON.stringify({ userId })
  })
}

// Onboarding APIs
export const saveDistilleryProfile = async (
  userId: number,
  profile: {
    name: string
    address: string
    city: string
    state: string
    postalCode: string
    dspNumber?: string
    ttbPermitId?: string
  }
): Promise<{ saved: boolean }> => {
  return apiFetch('/onboarding/profile', {
    method: 'POST',
    body: JSON.stringify({ userId, ...profile })
  })
}

export const connectIntegration = async (
  userId: number,
  integrationType: 'quickbooks' | 'bank'
): Promise<{ redirectUrl: string }> => {
  return apiFetch('/onboarding/connect-integration', {
    method: 'POST',
    body: JSON.stringify({ userId, integrationType })
  })
}

export const importBarrels = async (
  userId: number,
  csvData: string
): Promise<{ imported: number; errors: string[] }> => {
  return apiFetch('/onboarding/import-barrels', {
    method: 'POST',
    body: JSON.stringify({ userId, csvData })
  })
}

export const inviteTeamMembers = async (
  userId: number,
  invites: TeamInvite[]
): Promise<{ sent: number }> => {
  return apiFetch('/onboarding/invite-team', {
    method: 'POST',
    body: JSON.stringify({ userId, invites })
  })
}

export const completeOnboardingStep = async (
  userId: number,
  step: string
): Promise<{ completed: boolean }> => {
  return apiFetch('/onboarding/complete-step', {
    method: 'POST',
    body: JSON.stringify({ userId, step })
  })
}

export const getOnboardingProgress = async (
  userId: number
): Promise<{ completedSteps: string[]; progress: number }> => {
  return apiFetch(`/onboarding/progress/${userId}`)
}
