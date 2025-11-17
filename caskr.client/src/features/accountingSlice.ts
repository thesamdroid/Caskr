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

export type QuickBooksSyncStatus = 'Pending' | 'InProgress' | 'Success' | 'Failed'

export interface QuickBooksInvoiceSyncStatus {
  invoiceId: number
  status: QuickBooksSyncStatus | null
  qboInvoiceId?: string | null
  errorMessage?: string | null
  lastSyncedAt?: string | null
}

export interface QuickBooksInvoiceSyncResponse extends QuickBooksInvoiceSyncStatus {
  success: boolean
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

export const fetchInvoiceSyncStatus = createAsyncThunk(
  'accounting/fetchInvoiceSyncStatus',
  async (invoiceId: number) => {
    const response = await authorizedFetch(`api/accounting/quickbooks/invoice-status?invoiceId=${invoiceId}`)
    if (!response.ok) {
      throw new Error('Failed to load invoice sync status')
    }

    const payload = (await response.json()) as QuickBooksInvoiceSyncStatus
    return payload
  }
)

export const syncInvoice = createAsyncThunk(
  'accounting/syncInvoice',
  async (invoiceId: number, { rejectWithValue }) => {
    const response = await authorizedFetch('api/accounting/quickbooks/sync-invoice', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ invoiceId })
    })

    const payload = (await response.json().catch(() => ({}))) as Partial<QuickBooksInvoiceSyncResponse> & { message?: string }
    if (!response.ok) {
      const message = payload?.message ?? payload?.errorMessage ?? 'Failed to sync invoice'
      return rejectWithValue(message)
    }

    return {
      invoiceId,
      success: payload.success ?? true,
      status: payload.status ?? 'Success',
      qboInvoiceId: payload.qboInvoiceId ?? null,
      errorMessage: payload.errorMessage ?? null,
      lastSyncedAt: payload.lastSyncedAt ?? new Date().toISOString()
    } satisfies QuickBooksInvoiceSyncResponse
  }
)

interface AccountingState {
  status: QuickBooksConnectionStatus | null
  statusCompanyId: number | null
  statusLoading: boolean
  accounts: QuickBooksAccount[]
  accountsLoading: boolean
  mappings: QuickBooksAccountMapping[]
  mappingsLoading: boolean
  savingMappings: boolean
  connecting: boolean
  disconnecting: boolean
  error: string | null
  invoiceStatuses: Record<number, QuickBooksInvoiceSyncStatus>
  invoiceStatusLoading: Record<number, boolean>
  invoiceSyncing: Record<number, boolean>
  invoiceSyncErrors: Record<number, string | null>
}

const initialState: AccountingState = {
  status: null,
  statusCompanyId: null,
  statusLoading: false,
  accounts: [],
  accountsLoading: false,
  mappings: [],
  mappingsLoading: false,
  savingMappings: false,
  connecting: false,
  disconnecting: false,
  error: null,
  invoiceStatuses: {},
  invoiceStatusLoading: {},
  invoiceSyncing: {},
  invoiceSyncErrors: {}
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
        state.statusCompanyId = null
      })
      .addCase(fetchQuickBooksStatus.fulfilled, (state, action) => {
        state.status = action.payload
        state.statusLoading = false
        state.statusCompanyId = action.meta.arg
      })
      .addCase(fetchQuickBooksStatus.rejected, (state, action) => {
        state.statusLoading = false
        state.status = null
        state.statusCompanyId = null
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
        state.statusCompanyId = null
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

    builder
      .addCase(fetchInvoiceSyncStatus.pending, (state, action) => {
        const invoiceId = action.meta.arg
        state.invoiceStatusLoading[invoiceId] = true
        state.error = null
      })
      .addCase(fetchInvoiceSyncStatus.fulfilled, (state, action) => {
        const invoiceId = action.payload.invoiceId
        state.invoiceStatuses[invoiceId] = action.payload
        state.invoiceStatusLoading[invoiceId] = false
      })
      .addCase(fetchInvoiceSyncStatus.rejected, (state, action) => {
        const invoiceId = action.meta.arg
        state.invoiceStatusLoading[invoiceId] = false
        state.error = action.error.message ?? 'Unable to load invoice sync status'
      })

    builder
      .addCase(syncInvoice.pending, (state, action) => {
        const invoiceId = action.meta.arg
        state.invoiceSyncing[invoiceId] = true
        state.invoiceSyncErrors[invoiceId] = null
        state.error = null
      })
      .addCase(syncInvoice.fulfilled, (state, action) => {
        const invoiceId = action.payload.invoiceId
        state.invoiceSyncing[invoiceId] = false
        state.invoiceStatuses[invoiceId] = {
          invoiceId,
          status: action.payload.status ?? (action.payload.success ? 'Success' : null),
          qboInvoiceId: action.payload.qboInvoiceId ?? null,
          errorMessage: action.payload.errorMessage ?? null,
          lastSyncedAt: action.payload.lastSyncedAt
        }
        state.invoiceSyncErrors[invoiceId] = null
      })
      .addCase(syncInvoice.rejected, (state, action) => {
        const invoiceId = action.meta.arg
        state.invoiceSyncing[invoiceId] = false
        const message = (action.payload as string) ?? action.error.message ?? 'Unable to sync invoice'
        state.invoiceSyncErrors[invoiceId] = message
        state.error = message
      })
  }
})

export const { clearAccountingError } = accountingSlice.actions
export default accountingSlice.reducer
