import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import type { SignupStep, SignupFormData, PlanSelectionState } from '../types/signup'
import * as signupApi from '../api/signupApi'
import { setStoredAuth } from '../api/portalApi'

interface SignupState {
  currentStep: SignupStep
  formData: SignupFormData
  userId: number | null
  verification: {
    email: string
    attempts: number
    isLocked: boolean
    lastCodeSentAt: string | null
    codesSentCount: number
  }
  planSelection: PlanSelectionState
  isLoading: boolean
  error: string | null
  registrationComplete: boolean
}

const initialState: SignupState = {
  currentStep: 'register',
  formData: {
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    distilleryName: '',
    agreedToTerms: false
  },
  userId: null,
  verification: {
    email: '',
    attempts: 0,
    isLocked: false,
    lastCodeSentAt: null,
    codesSentCount: 0
  },
  planSelection: {
    selectedTierId: null,
    billingCycle: 'monthly',
    promoCode: null,
    discountedPrice: null
  },
  isLoading: false,
  error: null,
  registrationComplete: false
}

// Async thunks
export const checkEmail = createAsyncThunk(
  'signup/checkEmail',
  async (email: string, { rejectWithValue }) => {
    try {
      const result = await signupApi.checkEmailAvailability(email)
      if (!result.available) {
        return rejectWithValue('An account with this email already exists')
      }
      return result
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to check email')
    }
  }
)

export const register = createAsyncThunk(
  'signup/register',
  async (data: SignupFormData, { rejectWithValue }) => {
    try {
      return await signupApi.registerUser(data)
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Registration failed')
    }
  }
)

export const sendVerification = createAsyncThunk(
  'signup/sendVerification',
  async (email: string, { rejectWithValue, getState }) => {
    const state = getState() as { signup: SignupState }
    if (state.signup.verification.codesSentCount >= 3) {
      return rejectWithValue('Maximum resend attempts reached. Please try again later.')
    }
    try {
      return await signupApi.sendVerificationCode(email)
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to send verification code')
    }
  }
)

export const verifyCode = createAsyncThunk(
  'signup/verifyCode',
  async ({ email, code }: { email: string; code: string }, { rejectWithValue, getState }) => {
    const state = getState() as { signup: SignupState }
    if (state.signup.verification.isLocked) {
      return rejectWithValue('Account locked. Check email for instructions.')
    }
    try {
      const result = await signupApi.verifyCode(email, code)
      if (!result.verified) {
        return rejectWithValue('Invalid verification code')
      }
      return result
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Verification failed')
    }
  }
)

export const createSubscription = createAsyncThunk(
  'signup/createSubscription',
  async (
    data: {
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
    },
    { rejectWithValue }
  ) => {
    try {
      return await signupApi.createSubscription(data)
    } catch (error) {
      return rejectWithValue(
        error instanceof Error ? error.message : 'Payment processing failed. Your card was not charged.'
      )
    }
  }
)

export const completeSetup = createAsyncThunk(
  'signup/completeSetup',
  async (userId: number, { rejectWithValue }) => {
    try {
      const result = await signupApi.completeAccountSetup(userId)
      // Store auth tokens
      setStoredAuth(result.accessToken, result.user, result.expiresAt)
      return result
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to complete setup')
    }
  }
)

const signupSlice = createSlice({
  name: 'signup',
  initialState,
  reducers: {
    setStep: (state, action: PayloadAction<SignupStep>) => {
      state.currentStep = action.payload
      state.error = null
    },
    updateFormData: (state, action: PayloadAction<Partial<SignupFormData>>) => {
      state.formData = { ...state.formData, ...action.payload }
    },
    setPlanSelection: (state, action: PayloadAction<Partial<PlanSelectionState>>) => {
      state.planSelection = { ...state.planSelection, ...action.payload }
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload
    },
    clearError: (state) => {
      state.error = null
    },
    resetSignup: () => initialState,
    // Initialize from URL params
    initFromParams: (state, action: PayloadAction<{ plan?: string; promo?: string }>) => {
      if (action.payload.plan) {
        // Will be resolved to tier ID later
        state.planSelection.selectedTierId = null
      }
      if (action.payload.promo) {
        state.planSelection.promoCode = action.payload.promo
      }
    }
  },
  extraReducers: builder => {
    builder
      // Check email
      .addCase(checkEmail.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(checkEmail.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(checkEmail.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Register
      .addCase(register.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(register.fulfilled, (state, action) => {
        state.isLoading = false
        state.userId = action.payload.userId
        state.verification.email = action.payload.email
        state.currentStep = 'verify'
      })
      .addCase(register.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Send verification
      .addCase(sendVerification.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(sendVerification.fulfilled, state => {
        state.isLoading = false
        state.verification.lastCodeSentAt = new Date().toISOString()
        state.verification.codesSentCount++
      })
      .addCase(sendVerification.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Verify code
      .addCase(verifyCode.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(verifyCode.fulfilled, state => {
        state.isLoading = false
        state.currentStep = 'plan'
      })
      .addCase(verifyCode.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
        state.verification.attempts++
        if (state.verification.attempts >= 5) {
          state.verification.isLocked = true
        }
      })

      // Create subscription
      .addCase(createSubscription.pending, state => {
        state.isLoading = true
        state.error = null
        state.currentStep = 'creating'
      })
      .addCase(createSubscription.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(createSubscription.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
        state.currentStep = 'payment'
      })

      // Complete setup
      .addCase(completeSetup.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(completeSetup.fulfilled, state => {
        state.isLoading = false
        state.currentStep = 'complete'
        state.registrationComplete = true
      })
      .addCase(completeSetup.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
  }
})

export const {
  setStep,
  updateFormData,
  setPlanSelection,
  setError,
  clearError,
  resetSignup,
  initFromParams
} = signupSlice.actions

export default signupSlice.reducer
