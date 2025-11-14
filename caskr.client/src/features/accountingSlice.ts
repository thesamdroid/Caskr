import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface QuickBooksConnectionStatus {
  connected: boolean
  realmId?: string
  connectedAt?: string
}

export interface QuickBooksAccount {
  id: string
  name: string
  accountType: string
  active: boolean
}

export interface QuickBooksAccountMapping {
  caskrAccountType: string
  qboAccountId: string
  qboAccountName: string
}

export interface SaveMappingsPayload {
  companyId: number
  mappings: QuickBooksAccountMapping[]
}

export const fetchQuickBooksStatus = createAsyncThunk(
  'accounting/fetchStatus',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/accounting/quickbooks/status?companyId=${companyId}`)
    if (!response.ok) {
      throw new Error('Failed to load QuickBooks status')
    }
    return (await response.json()) as QuickBooksConnectionStatus
  }
)

export const connectQuickBooks = createAsyncThunk(
  'accounting/connect',
  async (companyId: number) => {
    const response = await authorizedFetch('api/accounting/quickbooks/connect', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ companyId })
    })

    if (!response.ok) {
      const message = (await response.json().catch(() => ({})))?.message ?? 'Failed to start QuickBooks connection'
      throw new Error(message)
    }

    return (await response.json()) as { authUrl: string }
  }
)

export const disconnectQuickBooks = createAsyncThunk(
  'accounting/disconnect',
  async (companyId: number) => {
    const response = await authorizedFetch('api/accounting/quickbooks/disconnect', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ companyId })
    })

    if (!response.ok) {
      const message = (await response.json().catch(() => ({})))?.message ?? 'Failed to disconnect QuickBooks'
      throw new Error(message)
    }
  }
)

export const fetchQboAccounts = createAsyncThunk(
  'accounting/fetchAccounts',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/accounting/quickbooks/accounts?companyId=${companyId}`)
    if (response.status === 404) {
      return [] as QuickBooksAccount[]
    }

    if (!response.ok) {
      throw new Error('Failed to load QuickBooks accounts')
    }

    return (await response.json()) as QuickBooksAccount[]
  }
)

export const fetchAccountMappings = createAsyncThunk(
  'accounting/fetchMappings',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/accounting/quickbooks/mappings?companyId=${companyId}`)
    if (response.status === 404) {
      return [] as QuickBooksAccountMapping[]
    }

    if (!response.ok) {
      throw new Error('Failed to load saved mappings')
    }

    return (await response.json()) as QuickBooksAccountMapping[]
  }
)

export const saveAccountMappings = createAsyncThunk(
  'accounting/saveMappings',
  async (payload: SaveMappingsPayload) => {
    const response = await authorizedFetch('api/accounting/quickbooks/mappings', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (!response.ok) {
      const message = (await response.json().catch(() => ({})))?.message ?? 'Failed to save mappings'
      throw new Error(message)
    }

    return (await response.json()) as QuickBooksAccountMapping[]
  }
)

interface AccountingState {
  status: QuickBooksConnectionStatus | null
  statusLoading: boolean
  accounts: QuickBooksAccount[]
  accountsLoading: boolean
  mappings: QuickBooksAccountMapping[]
  mappingsLoading: boolean
  savingMappings: boolean
  connecting: boolean
  disconnecting: boolean
  error: string | null
}

const initialState: AccountingState = {
  status: null,
  statusLoading: false,
  accounts: [],
  accountsLoading: false,
  mappings: [],
  mappingsLoading: false,
  savingMappings: false,
  connecting: false,
  disconnecting: false,
  error: null
}

const accountingSlice = createSlice({
  name: 'accounting',
  initialState,
  reducers: {
    clearAccountingError(state) {
      state.error = null
    }
  },
  extraReducers: builder => {
    builder
      .addCase(fetchQuickBooksStatus.pending, state => {
        state.statusLoading = true
        state.error = null
      })
      .addCase(fetchQuickBooksStatus.fulfilled, (state, action) => {
        state.status = action.payload
        state.statusLoading = false
      })
      .addCase(fetchQuickBooksStatus.rejected, (state, action) => {
        state.statusLoading = false
        state.status = null
        state.error = action.error.message ?? 'Unable to load QuickBooks status'
      })

    builder
      .addCase(connectQuickBooks.pending, state => {
        state.connecting = true
        state.error = null
      })
      .addCase(connectQuickBooks.fulfilled, state => {
        state.connecting = false
      })
      .addCase(connectQuickBooks.rejected, (state, action) => {
        state.connecting = false
        state.error = action.error.message ?? 'Unable to connect to QuickBooks'
      })

    builder
      .addCase(disconnectQuickBooks.pending, state => {
        state.disconnecting = true
        state.error = null
      })
      .addCase(disconnectQuickBooks.fulfilled, state => {
        state.disconnecting = false
        state.status = { connected: false }
        state.mappings = []
      })
      .addCase(disconnectQuickBooks.rejected, (state, action) => {
        state.disconnecting = false
        state.error = action.error.message ?? 'Unable to disconnect QuickBooks'
      })

    builder
      .addCase(fetchQboAccounts.pending, state => {
        state.accountsLoading = true
        state.error = null
      })
      .addCase(fetchQboAccounts.fulfilled, (state, action) => {
        state.accounts = action.payload
        state.accountsLoading = false
      })
      .addCase(fetchQboAccounts.rejected, (state, action) => {
        state.accountsLoading = false
        state.accounts = []
        state.error = action.error.message ?? 'Unable to load QuickBooks accounts'
      })

    builder
      .addCase(fetchAccountMappings.pending, state => {
        state.mappingsLoading = true
        state.error = null
      })
      .addCase(fetchAccountMappings.fulfilled, (state, action) => {
        state.mappings = action.payload
        state.mappingsLoading = false
      })
      .addCase(fetchAccountMappings.rejected, (state, action) => {
        state.mappingsLoading = false
        state.mappings = []
        state.error = action.error.message ?? 'Unable to load saved mappings'
      })

    builder
      .addCase(saveAccountMappings.pending, state => {
        state.savingMappings = true
        state.error = null
      })
      .addCase(saveAccountMappings.fulfilled, (state, action) => {
        state.savingMappings = false
        state.mappings = action.payload
      })
      .addCase(saveAccountMappings.rejected, (state, action) => {
        state.savingMappings = false
        state.error = action.error.message ?? 'Unable to save mappings'
      })
  }
})

export const { clearAccountingError } = accountingSlice.actions
export default accountingSlice.reducer
