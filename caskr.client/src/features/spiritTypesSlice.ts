import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface SpiritType {
  id: number
  name: string
}

export const fetchSpiritTypes = createAsyncThunk('spiritTypes/fetchSpiritTypes', async () => {
  const response = await authorizedFetch('api/spirittypes')
  if (!response.ok) throw new Error('Failed to fetch spirit types')
  return (await response.json()) as SpiritType[]
})

interface SpiritTypesState {
  items: SpiritType[]
}

const initialState: SpiritTypesState = { items: [] }

const spiritTypesSlice = createSlice({
  name: 'spiritTypes',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchSpiritTypes.fulfilled, (state, action) => {
      state.items = action.payload
    })
  }
})

export default spiritTypesSlice.reducer
