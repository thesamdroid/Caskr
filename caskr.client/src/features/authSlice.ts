import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'
import { AuthUser, LoginRequestDto, LoginResponseDto } from '../types/auth'

const STORAGE_KEYS = {
  token: 'token',
  refreshToken: 'refreshToken',
  user: 'auth.user',
  expiresAt: 'auth.expiresAt'
}

export const TTB_COMPLIANCE_PERMISSION = 'TTB_COMPLIANCE'

interface AuthState {
  user: AuthUser | null
  token: string | null
  refreshToken: string | null
  expiresAt: string | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}

const getInitialState = (): AuthState => {
  if (typeof window === 'undefined') {
    return {
      user: null,
      token: null,
      refreshToken: null,
      expiresAt: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    }
  }

  try {
    const token = localStorage.getItem(STORAGE_KEYS.token)
    const refreshToken = localStorage.getItem(STORAGE_KEYS.refreshToken)
    const expiresAt = localStorage.getItem(STORAGE_KEYS.expiresAt)
    const userJson = localStorage.getItem(STORAGE_KEYS.user)
    const user = userJson ? (JSON.parse(userJson) as AuthUser) : null

    return {
      user,
      token,
      refreshToken,
      expiresAt,
      isAuthenticated: Boolean(token && user),
      isLoading: false,
      error: null
    }
  } catch (error) {
    console.error('[authSlice] Failed to hydrate auth state from storage', error)
    return {
      user: null,
      token: null,
      refreshToken: null,
      expiresAt: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    }
  }
}

const persistAuthState = (state: AuthState) => {
  if (typeof window === 'undefined') return
  if (!state.user || !state.token) return

  localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(state.user))
  localStorage.setItem(STORAGE_KEYS.token, state.token)
  if (state.refreshToken) {
    localStorage.setItem(STORAGE_KEYS.refreshToken, state.refreshToken)
  }
  if (state.expiresAt) {
    localStorage.setItem(STORAGE_KEYS.expiresAt, state.expiresAt)
  }
}

const clearAuthStorage = () => {
  if (typeof window === 'undefined') return
  localStorage.removeItem(STORAGE_KEYS.user)
  localStorage.removeItem(STORAGE_KEYS.token)
  localStorage.removeItem(STORAGE_KEYS.refreshToken)
  localStorage.removeItem(STORAGE_KEYS.expiresAt)
}

export const login = createAsyncThunk<LoginResponseDto, LoginRequestDto, { rejectValue: string }>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    const response = await authorizedFetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    })

    if (!response.ok) {
      return rejectWithValue('Invalid email or password')
    }

    return (await response.json()) as LoginResponseDto
  }
)

const authSlice = createSlice({
  name: 'auth',
  initialState: getInitialState(),
  reducers: {
    hydrateFromStorage: state => {
      const hydrated = getInitialState()
      state.user = hydrated.user
      state.token = hydrated.token
      state.refreshToken = hydrated.refreshToken
      state.expiresAt = hydrated.expiresAt
      state.isAuthenticated = hydrated.isAuthenticated
      state.error = hydrated.error
    },
    logout: state => {
      state.user = null
      state.token = null
      state.refreshToken = null
      state.expiresAt = null
      state.isAuthenticated = false
      clearAuthStorage()
    },
    setUser: (state, action: PayloadAction<AuthUser | null>) => {
      state.user = action.payload
      state.isAuthenticated = Boolean(state.token && action.payload)
      if (state.user && state.token) {
        persistAuthState(state)
      }
    }
  },
  extraReducers: builder => {
    builder
      .addCase(login.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(login.fulfilled, (state, action) => {
        const { token, refreshToken, expiresAt, user } = action.payload
        state.token = token
        state.refreshToken = refreshToken
        state.expiresAt = expiresAt
        state.user = user
        state.isAuthenticated = Boolean(token && user)
        state.isLoading = false
        state.error = null
        persistAuthState(state)
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Login failed'
      })
  }
})

export const { hydrateFromStorage, logout, setUser } = authSlice.actions

export const userHasPermission = (user: AuthUser | null, permission: string) => {
  if (!user) return false
  return user.permissions.some(value => value.toUpperCase() === permission.toUpperCase())
}

export default authSlice.reducer
