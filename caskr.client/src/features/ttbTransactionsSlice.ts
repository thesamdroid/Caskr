import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export enum TtbTransactionType {
  Production = 0,
  TransferIn = 1,
  TransferOut = 2,
  Loss = 3,
  Gain = 4,
  TaxDetermination = 5,
  Destruction = 6,
  Bottling = 7
}

export enum TtbSpiritsType {
  Under190Proof = 0,
  Neutral190OrMore = 1,
  Alcohol = 2,
  Wine = 3
}

export interface TtbTransaction {
  id: number
  companyId: number
  transactionDate: string
  transactionType: TtbTransactionType
  productType: string
  spiritsType: TtbSpiritsType
  proofGallons: number
  wineGallons: number
  sourceEntityType?: string | null
  sourceEntityId?: number | null
  notes?: string | null
}

export interface CreateTtbTransactionRequest {
  companyId: number
  transactionDate: string
  transactionType: TtbTransactionType
  productType: string
  spiritsType: TtbSpiritsType
  proofGallons: number
  wineGallons: number
  notes?: string
}

export interface UpdateTtbTransactionRequest {
  transactionDate: string
  transactionType: TtbTransactionType
  productType: string
  spiritsType: TtbSpiritsType
  proofGallons: number
  wineGallons: number
  notes?: string
}

interface FetchParams {
  companyId: number
  month: number
  year: number
}

interface TtbTransactionsState {
  items: TtbTransaction[]
  isLoading: boolean
  error: string | null
  selectedMonth: number
  selectedYear: number
}

interface TtbTransactionPayload {
  id: number
  companyId: number
  transactionDate: string
  transactionType: TtbTransactionType
  productType: string
  spiritsType: TtbSpiritsType
  proofGallons: number
  wineGallons: number
  sourceEntityType?: string | null
  sourceEntityId?: number | null
  notes?: string | null
}

const normalizeTransaction = (payload: TtbTransactionPayload): TtbTransaction => ({
  id: payload.id,
  companyId: payload.companyId,
  transactionDate: payload.transactionDate,
  transactionType: payload.transactionType,
  productType: payload.productType,
  spiritsType: payload.spiritsType,
  proofGallons: payload.proofGallons,
  wineGallons: payload.wineGallons,
  sourceEntityType: payload.sourceEntityType ?? null,
  sourceEntityId: payload.sourceEntityId ?? null,
  notes: payload.notes ?? null
})

export const fetchTtbTransactions = createAsyncThunk(
  'ttbTransactions/fetch',
  async ({ companyId, month, year }: FetchParams) => {
    const params = new URLSearchParams({
      companyId: companyId.toString(),
      month: month.toString(),
      year: year.toString()
    })

    const response = await authorizedFetch(`/api/ttb/transactions?${params.toString()}`)
    if (!response.ok) {
      console.error('[ttbTransactionsSlice] Failed to fetch transactions', { status: response.status })
      throw new Error('Unable to load TTB transactions')
    }

    const content = await response.json()
    if (!Array.isArray(content)) {
      console.warn('[ttbTransactionsSlice] Unexpected response shape while fetching transactions', content)
      return [] as TtbTransaction[]
    }

    return content.map(normalizeTransaction)
  }
)

export const createTtbTransaction = createAsyncThunk(
  'ttbTransactions/create',
  async (request: CreateTtbTransactionRequest) => {
    const response = await authorizedFetch('/api/ttb/transactions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbTransactionsSlice] Failed to create transaction', { status: response.status, error: errorText })
      throw new Error('Unable to create TTB transaction')
    }

    const content = await response.json()
    return normalizeTransaction(content)
  }
)

export const updateTtbTransaction = createAsyncThunk(
  'ttbTransactions/update',
  async ({ id, request }: { id: number; request: UpdateTtbTransactionRequest }) => {
    const response = await authorizedFetch(`/api/ttb/transactions/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbTransactionsSlice] Failed to update transaction', { status: response.status, error: errorText })
      throw new Error('Unable to update TTB transaction')
    }

    const content = await response.json()
    return normalizeTransaction(content)
  }
)

export const deleteTtbTransaction = createAsyncThunk(
  'ttbTransactions/delete',
  async (id: number) => {
    const response = await authorizedFetch(`/api/ttb/transactions/${id}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbTransactionsSlice] Failed to delete transaction', { status: response.status, error: errorText })
      throw new Error('Unable to delete TTB transaction')
    }

    return id
  }
)

const currentDate = new Date()
const initialState: TtbTransactionsState = {
  items: [],
  isLoading: false,
  error: null,
  selectedMonth: currentDate.getMonth() + 1,
  selectedYear: currentDate.getFullYear()
}

const ttbTransactionsSlice = createSlice({
  name: 'ttbTransactions',
  initialState,
  reducers: {
    setSelectedMonth: (state, action) => {
      state.selectedMonth = action.payload
    },
    setSelectedYear: (state, action) => {
      state.selectedYear = action.payload
    }
  },
  extraReducers: builder => {
    builder
      // Fetch
      .addCase(fetchTtbTransactions.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchTtbTransactions.fulfilled, (state, action) => {
        state.items = action.payload
        state.isLoading = false
      })
      .addCase(fetchTtbTransactions.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to load TTB transactions'
      })
      // Create
      .addCase(createTtbTransaction.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(createTtbTransaction.fulfilled, (state, action) => {
        state.items.unshift(action.payload)
        state.isLoading = false
      })
      .addCase(createTtbTransaction.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to create TTB transaction'
      })
      // Update
      .addCase(updateTtbTransaction.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(updateTtbTransaction.fulfilled, (state, action) => {
        const index = state.items.findIndex(t => t.id === action.payload.id)
        if (index !== -1) {
          state.items[index] = action.payload
        }
        state.isLoading = false
      })
      .addCase(updateTtbTransaction.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to update TTB transaction'
      })
      // Delete
      .addCase(deleteTtbTransaction.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(deleteTtbTransaction.fulfilled, (state, action) => {
        state.items = state.items.filter(t => t.id !== action.payload)
        state.isLoading = false
      })
      .addCase(deleteTtbTransaction.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to delete TTB transaction'
      })
  }
})

export const { setSelectedMonth, setSelectedYear } = ttbTransactionsSlice.actions
export default ttbTransactionsSlice.reducer
