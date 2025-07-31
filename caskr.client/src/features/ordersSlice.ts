import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'

export interface Order {
  id: number
  name: string
  statusId: number
}

export const fetchOrders = createAsyncThunk('orders/fetchOrders', async () => {
  const response = await fetch('api/orders')
  if (!response.ok) throw new Error('Failed to fetch orders')
  return (await response.json()) as Order[]
})

interface OrdersState {
  items: Order[]
}

const initialState: OrdersState = { items: [] }

const ordersSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchOrders.fulfilled, (state, action) => {
      state.items = action.payload
    })
  }
})

export default ordersSlice.reducer

