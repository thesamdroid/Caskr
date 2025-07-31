import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'

export interface Status {
  id: number
  name: string
}

export const fetchStatuses = createAsyncThunk('statuses/fetchStatuses', async () => {
  const response = await fetch('api/status')
  if (!response.ok) throw new Error('Failed to fetch statuses')
  return (await response.json()) as Status[]
})

interface StatusState {
  items: Status[]
}

const initialState: StatusState = { items: [] }

const statusSlice = createSlice({
  name: 'statuses',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchStatuses.fulfilled, (state, action) => {
      state.items = action.payload
    })
  }
})

export default statusSlice.reducer

