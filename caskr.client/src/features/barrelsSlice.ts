import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface Barrel {
  id: number
  sku: string
  orderId: number
  companyId: number
  batchId: number
  rickhouseId: number
  warehouseId: number
}

export const fetchBarrels = createAsyncThunk(
  'barrels/fetchBarrels',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/barrels/company/${companyId}`)
    if (!response.ok) throw new Error('Failed to fetch barrels')
    return (await response.json()) as Barrel[]
  }
)

export interface ForecastParams {
  companyId: number
  targetDate: string
  ageYears: number
}

export const forecastBarrels = createAsyncThunk(
  'barrels/forecast',
  async ({ companyId, targetDate, ageYears }: ForecastParams) => {
    const response = await authorizedFetch(
      `api/barrels/company/${companyId}/forecast?targetDate=${encodeURIComponent(targetDate)}&ageYears=${ageYears}`
    )
    if (!response.ok) throw new Error('Failed to forecast barrels')
    return (await response.json()) as { barrels: Barrel[]; count: number }
  }
)

export interface BarrelImportParams {
  companyId: number
  file: File
  batchId?: number
  mashBillId?: number
}

export const importBarrels = createAsyncThunk(
  'barrels/import',
  async ({ companyId, file, batchId, mashBillId }: BarrelImportParams, { rejectWithValue }) => {
    const formData = new FormData()
    formData.append('file', file)
    if (batchId !== undefined) {
      formData.append('batchId', batchId.toString())
    }
    if (mashBillId !== undefined) {
      formData.append('mashBillId', mashBillId.toString())
    }

    const response = await authorizedFetch(`api/barrels/company/${companyId}/import`, {
      method: 'POST',
      body: formData
    })

    if (!response.ok) {
      console.warn('[barrelsSlice] Import barrels request failed', {
        status: response.status,
        statusText: response.statusText
      })
      try {
        const error = await response.json()
        console.warn('[barrelsSlice] Import barrels error response', error)
        return rejectWithValue(error)
      } catch (err) {
        console.error('[barrelsSlice] Failed to parse barrel import error response', err)
        return rejectWithValue({ message: 'Failed to import barrels' })
      }
    }

    console.log('[barrelsSlice] Barrel import completed successfully', {
      status: response.status,
      statusText: response.statusText
    })
    return (await response.json()) as { created: number; batchId: number; createdNewBatch: boolean }
  }
)

interface BarrelsState {
  items: Barrel[]
  forecast: Barrel[]
  forecastCount: number
  forecastDate: string | null
  forecastAgeYears: number | null
}

const initialState: BarrelsState = {
  items: [],
  forecast: [],
  forecastCount: 0,
  forecastDate: null,
  forecastAgeYears: null
}

const barrelsSlice = createSlice({
  name: 'barrels',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchBarrels.fulfilled, (state, action) => {
      state.items = action.payload
    })
    builder.addCase(forecastBarrels.fulfilled, (state, action) => {
      state.forecast = action.payload.barrels
      state.forecastCount = action.payload.count
      state.forecastDate = action.meta.arg.targetDate
      state.forecastAgeYears = action.meta.arg.ageYears
    })
  }
})

export default barrelsSlice.reducer
