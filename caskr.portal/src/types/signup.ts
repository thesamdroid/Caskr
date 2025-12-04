// Signup and Onboarding Types

export interface SignupFormData {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  distilleryName: string
  agreedToTerms: boolean
}

export interface VerificationState {
  email: string
  code: string
  attempts: number
  isLocked: boolean
  lastCodeSentAt: Date | null
  codesSentCount: number
}

export interface PlanSelectionState {
  selectedTierId: number | null
  billingCycle: 'monthly' | 'annual'
  promoCode: string | null
  discountedPrice: number | null
}

export interface PaymentFormData {
  cardholderName: string
  billingAddress: {
    line1: string
    line2?: string
    city: string
    state: string
    postalCode: string
    country: string
  }
}

export interface OnboardingFormData {
  // Step 1: Distillery Profile
  distilleryProfile: {
    name: string
    address: string
    city: string
    state: string
    postalCode: string
    dspNumber?: string
    ttbPermitId?: string
  }
  // Step 2: Integrations
  integrations: {
    quickbooksConnected: boolean
    bankFeedsConnected: boolean
  }
  // Step 3: Initial Data
  initialData: {
    importMethod: 'csv' | 'manual' | 'skip'
    barrelCount?: number
  }
  // Step 4: Team Members
  teamInvites: TeamInvite[]
}

export interface TeamInvite {
  email: string
  role: 'admin' | 'manager' | 'viewer'
}

export type SignupStep =
  | 'register'
  | 'verify'
  | 'plan'
  | 'payment'
  | 'creating'
  | 'complete'

export type OnboardingStep =
  | 'profile'
  | 'integrations'
  | 'import'
  | 'team'
  | 'tour'
  | 'complete'

export interface SignupState {
  currentStep: SignupStep
  formData: SignupFormData
  verification: VerificationState
  planSelection: PlanSelectionState
  payment: PaymentFormData
  isLoading: boolean
  error: string | null
}

export interface OnboardingState {
  currentStep: OnboardingStep
  completedSteps: OnboardingStep[]
  formData: OnboardingFormData
  isLoading: boolean
  error: string | null
}

// Password strength calculation
export interface PasswordStrength {
  score: 0 | 1 | 2 | 3 | 4
  label: 'Very Weak' | 'Weak' | 'Fair' | 'Strong' | 'Very Strong'
  suggestions: string[]
}

export function calculatePasswordStrength(password: string): PasswordStrength {
  let score = 0
  const suggestions: string[] = []

  if (password.length >= 8) score++
  else suggestions.push('Use at least 8 characters')

  if (password.length >= 12) score++

  if (/[a-z]/.test(password)) score++
  else suggestions.push('Add lowercase letters')

  if (/[A-Z]/.test(password)) score++
  else suggestions.push('Add uppercase letters')

  if (/[0-9]/.test(password)) score++
  else suggestions.push('Add numbers')

  if (/[^a-zA-Z0-9]/.test(password)) score++
  else suggestions.push('Add special characters')

  // Normalize to 0-4 scale
  const normalizedScore = Math.min(4, Math.floor(score * 0.8)) as 0 | 1 | 2 | 3 | 4

  const labels: Record<number, PasswordStrength['label']> = {
    0: 'Very Weak',
    1: 'Weak',
    2: 'Fair',
    3: 'Strong',
    4: 'Very Strong'
  }

  return {
    score: normalizedScore,
    label: labels[normalizedScore],
    suggestions
  }
}

// Email validation
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

// Password validation
export function validatePassword(password: string): { valid: boolean; errors: string[] } {
  const errors: string[] = []

  if (password.length < 8) {
    errors.push('Password must be at least 8 characters')
  }
  if (!/[A-Z]/.test(password)) {
    errors.push('Password must contain at least one uppercase letter')
  }
  if (!/[a-z]/.test(password)) {
    errors.push('Password must contain at least one lowercase letter')
  }
  if (!/[0-9]/.test(password)) {
    errors.push('Password must contain at least one number')
  }

  return { valid: errors.length === 0, errors }
}
