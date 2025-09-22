import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface Barrel {
  id: number
  sku: string
  orderId: number
  companyId: number
  batchId: number
  rickhouseId: number
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

interface BarrelsState {
  items: Barrel[]
  forecast: Barrel[]
  forecastCount: number
}

const initialState: BarrelsState = { items: [], forecast: [], forecastCount: 0 }

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
    })
  }
})

export default barrelsSlice.reducer
