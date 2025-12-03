import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import {
  PortalUser,
  PortalAuthState,
  PortalLoginRequest,
  PortalLoginResponse,
  PortalRegistrationRequest,
  PortalRegistrationResponse
} from '../types/portal'
import {
  authApi,
  setStoredAuth,
  clearStoredAuth,
  getStoredToken,
  getStoredUser,
  getStoredExpiresAt
} from '../api/portalApi'

const getInitialState = (): PortalAuthState => {
  if (typeof window === 'undefined') {
    return {
      user: null,
      token: null,
      expiresAt: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    }
  }

  try {
    const token = getStoredToken()
    const user = getStoredUser()
    const expiresAt = getStoredExpiresAt()

    // Check if token is expired
    if (expiresAt && new Date(expiresAt) < new Date()) {
      clearStoredAuth()
      return {
        user: null,
        token: null,
        expiresAt: null,
        isAuthenticated: false,
        isLoading: false,
        error: null
      }
    }

    return {
      user: user as PortalUser | null,
      token,
      expiresAt,
      isAuthenticated: Boolean(token && user),
      isLoading: false,
      error: null
    }
  } catch {
    return {
      user: null,
      token: null,
      expiresAt: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    }
  }
}

export const login = createAsyncThunk<
  PortalLoginResponse,
  PortalLoginRequest,
  { rejectValue: string }
>('auth/login', async (credentials, { rejectWithValue }) => {
  try {
    const response = await authApi.login(credentials.email, credentials.password)
    setStoredAuth(response.accessToken, response.user, response.expiresAt)
    return response as PortalLoginResponse
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Login failed')
  }
})

export const register = createAsyncThunk<
  PortalRegistrationResponse,
  PortalRegistrationRequest,
  { rejectValue: string }
>('auth/register', async (data, { rejectWithValue }) => {
  try {
    const response = await authApi.register(data)
    return response
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Registration failed')
  }
})

export const forgotPassword = createAsyncThunk<
  { message: string },
  string,
  { rejectValue: string }
>('auth/forgotPassword', async (email, { rejectWithValue }) => {
  try {
    return await authApi.forgotPassword(email)
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Request failed')
  }
})

export const resetPassword = createAsyncThunk<
  { message: string },
  { token: string; newPassword: string },
  { rejectValue: string }
>('auth/resetPassword', async ({ token, newPassword }, { rejectWithValue }) => {
  try {
    return await authApi.resetPassword(token, newPassword)
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Password reset failed')
  }
})

export const verifyEmail = createAsyncThunk<
  { success: boolean; message: string },
  string,
  { rejectValue: string }
>('auth/verifyEmail', async (token, { rejectWithValue }) => {
  try {
    return await authApi.verifyEmail(token)
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Verification failed')
  }
})

const authSlice = createSlice({
  name: 'auth',
  initialState: getInitialState(),
  reducers: {
    logout: state => {
      state.user = null
      state.token = null
      state.expiresAt = null
      state.isAuthenticated = false
      state.error = null
      clearStoredAuth()
    },
    clearError: state => {
      state.error = null
    },
    setUser: (state, action: PayloadAction<PortalUser | null>) => {
      state.user = action.payload
      state.isAuthenticated = Boolean(state.token && action.payload)
    }
  },
  extraReducers: builder => {
    builder
      // Login
      .addCase(login.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(login.fulfilled, (state, action) => {
        state.token = action.payload.accessToken
        state.expiresAt = action.payload.expiresAt
        state.user = action.payload.user
        state.isAuthenticated = true
        state.isLoading = false
        state.error = null
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Login failed'
      })
      // Register
      .addCase(register.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(register.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(register.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Registration failed'
      })
      // Forgot password
      .addCase(forgotPassword.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(forgotPassword.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(forgotPassword.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Request failed'
      })
      // Reset password
      .addCase(resetPassword.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(resetPassword.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(resetPassword.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Password reset failed'
      })
      // Verify email
      .addCase(verifyEmail.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(verifyEmail.fulfilled, state => {
        state.isLoading = false
      })
      .addCase(verifyEmail.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Verification failed'
      })
  }
})

export const { logout, clearError, setUser } = authSlice.actions
export default authSlice.reducer
