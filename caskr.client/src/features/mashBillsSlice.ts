import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'

export interface MashBill {
  id: number
  name: string
}

export const fetchMashBills = createAsyncThunk('mashBills/fetchMashBills', async () => {
  const response = await fetch('api/mashbills')
  if (!response.ok) throw new Error('Failed to fetch mash bills')
  return (await response.json()) as MashBill[]
})

interface MashBillsState {
  items: MashBill[]
}

const initialState: MashBillsState = { items: [] }

const mashBillsSlice = createSlice({
  name: 'mashBills',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchMashBills.fulfilled, (state, action) => {
      state.items = action.payload
    })
  }
})

export default mashBillsSlice.reducer
