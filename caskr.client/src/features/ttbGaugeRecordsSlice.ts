import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export enum TtbGaugeType {
  Fill = 0,
  Storage = 1,
  Removal = 2
}

export interface TtbGaugeRecord {
  id: number
  barrelId: number
  barrelSku?: string | null
  gaugeDate: string
  gaugeType: TtbGaugeType
  proof: number
  temperature: number
  wineGallons: number
  proofGallons: number
  gaugedByUserId?: number | null
  gaugedByUserName?: string | null
  notes?: string | null
}

export interface CreateTtbGaugeRecordRequest {
  barrelId: number
  gaugeType: TtbGaugeType
  proof: number
  temperature: number
  wineGallons: number
  notes?: string
}

export interface UpdateTtbGaugeRecordRequest {
  proof: number
  temperature: number
  wineGallons: number
  notes?: string
}

interface FetchForBarrelParams {
  barrelId: number
}

interface FetchForCompanyParams {
  companyId: number
  startDate?: string
  endDate?: string
}

interface TtbGaugeRecordsState {
  items: TtbGaugeRecord[]
  isLoading: boolean
  error: string | null
  selectedBarrelId: number | null
}

interface TtbGaugeRecordPayload {
  id: number
  barrelId: number
  barrelSku?: string | null
  gaugeDate: string
  gaugeType: TtbGaugeType
  proof: number
  temperature: number
  wineGallons: number
  proofGallons: number
  gaugedByUserId?: number | null
  gaugedByUserName?: string | null
  notes?: string | null
}

const normalizeGaugeRecord = (payload: TtbGaugeRecordPayload): TtbGaugeRecord => ({
  id: payload.id,
  barrelId: payload.barrelId,
  barrelSku: payload.barrelSku ?? null,
  gaugeDate: payload.gaugeDate,
  gaugeType: payload.gaugeType,
  proof: payload.proof,
  temperature: payload.temperature,
  wineGallons: payload.wineGallons,
  proofGallons: payload.proofGallons,
  gaugedByUserId: payload.gaugedByUserId ?? null,
  gaugedByUserName: payload.gaugedByUserName ?? null,
  notes: payload.notes ?? null
})

export const fetchTtbGaugeRecordsForBarrel = createAsyncThunk(
  'ttbGaugeRecords/fetchForBarrel',
  async ({ barrelId }: FetchForBarrelParams) => {
    const response = await authorizedFetch(`/api/ttb/gauge-records/barrel/${barrelId}`)
    if (!response.ok) {
      console.error('[ttbGaugeRecordsSlice] Failed to fetch gauge records for barrel', { status: response.status })
      throw new Error('Unable to load gauge records')
    }

    const content = await response.json()
    if (!Array.isArray(content)) {
      console.warn('[ttbGaugeRecordsSlice] Unexpected response shape while fetching gauge records', content)
      return [] as TtbGaugeRecord[]
    }

    return content.map(normalizeGaugeRecord)
  }
)

export const fetchTtbGaugeRecordsForCompany = createAsyncThunk(
  'ttbGaugeRecords/fetchForCompany',
  async ({ companyId, startDate, endDate }: FetchForCompanyParams) => {
    const params = new URLSearchParams({ companyId: companyId.toString() })
    if (startDate) params.append('startDate', startDate)
    if (endDate) params.append('endDate', endDate)

    const response = await authorizedFetch(`/api/ttb/gauge-records/company/${companyId}?${params.toString()}`)
    if (!response.ok) {
      console.error('[ttbGaugeRecordsSlice] Failed to fetch gauge records for company', { status: response.status })
      throw new Error('Unable to load gauge records')
    }

    const content = await response.json()
    if (!Array.isArray(content)) {
      console.warn('[ttbGaugeRecordsSlice] Unexpected response shape while fetching gauge records', content)
      return [] as TtbGaugeRecord[]
    }

    return content.map(normalizeGaugeRecord)
  }
)

export const createTtbGaugeRecord = createAsyncThunk(
  'ttbGaugeRecords/create',
  async (request: CreateTtbGaugeRecordRequest) => {
    const response = await authorizedFetch('/api/ttb/gauge-records', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbGaugeRecordsSlice] Failed to create gauge record', { status: response.status, error: errorText })
      throw new Error('Unable to create gauge record')
    }

    const content = await response.json()
    return normalizeGaugeRecord(content)
  }
)

export const updateTtbGaugeRecord = createAsyncThunk(
  'ttbGaugeRecords/update',
  async ({ id, request }: { id: number; request: UpdateTtbGaugeRecordRequest }) => {
    const response = await authorizedFetch(`/api/ttb/gauge-records/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbGaugeRecordsSlice] Failed to update gauge record', { status: response.status, error: errorText })
      throw new Error('Unable to update gauge record')
    }

    const content = await response.json()
    return normalizeGaugeRecord(content)
  }
)

export const deleteTtbGaugeRecord = createAsyncThunk(
  'ttbGaugeRecords/delete',
  async (id: number) => {
    const response = await authorizedFetch(`/api/ttb/gauge-records/${id}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('[ttbGaugeRecordsSlice] Failed to delete gauge record', { status: response.status, error: errorText })
      throw new Error('Unable to delete gauge record')
    }

    return id
  }
)

const initialState: TtbGaugeRecordsState = {
  items: [],
  isLoading: false,
  error: null,
  selectedBarrelId: null
}

const ttbGaugeRecordsSlice = createSlice({
  name: 'ttbGaugeRecords',
  initialState,
  reducers: {
    setSelectedBarrelId: (state, action) => {
      state.selectedBarrelId = action.payload
    },
    clearGaugeRecords: state => {
      state.items = []
      state.error = null
    }
  },
  extraReducers: builder => {
    builder
      // Fetch for barrel
      .addCase(fetchTtbGaugeRecordsForBarrel.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchTtbGaugeRecordsForBarrel.fulfilled, (state, action) => {
        state.items = action.payload
        state.isLoading = false
      })
      .addCase(fetchTtbGaugeRecordsForBarrel.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to load gauge records'
      })
      // Fetch for company
      .addCase(fetchTtbGaugeRecordsForCompany.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchTtbGaugeRecordsForCompany.fulfilled, (state, action) => {
        state.items = action.payload
        state.isLoading = false
      })
      .addCase(fetchTtbGaugeRecordsForCompany.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to load gauge records'
      })
      // Create
      .addCase(createTtbGaugeRecord.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(createTtbGaugeRecord.fulfilled, (state, action) => {
        state.items.unshift(action.payload)
        state.isLoading = false
      })
      .addCase(createTtbGaugeRecord.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to create gauge record'
      })
      // Update
      .addCase(updateTtbGaugeRecord.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(updateTtbGaugeRecord.fulfilled, (state, action) => {
        const index = state.items.findIndex(r => r.id === action.payload.id)
        if (index !== -1) {
          state.items[index] = action.payload
        }
        state.isLoading = false
      })
      .addCase(updateTtbGaugeRecord.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to update gauge record'
      })
      // Delete
      .addCase(deleteTtbGaugeRecord.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(deleteTtbGaugeRecord.fulfilled, (state, action) => {
        state.items = state.items.filter(r => r.id !== action.payload)
        state.isLoading = false
      })
      .addCase(deleteTtbGaugeRecord.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to delete gauge record'
      })
  }
})

export const { setSelectedBarrelId, clearGaugeRecords } = ttbGaugeRecordsSlice.actions
export default ttbGaugeRecordsSlice.reducer
