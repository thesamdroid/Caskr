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

export const addOrder = createAsyncThunk('orders/addOrder', async (order: Omit<Order, 'id'>) => {
  const response = await fetch('api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  })
  if (!response.ok) throw new Error('Failed to add order')
  return (await response.json()) as Order
})

export const updateOrder = createAsyncThunk('orders/updateOrder', async (order: Order) => {
  const response = await fetch(`api/orders/${order.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  })
  if (!response.ok) throw new Error('Failed to update order')
  return (await response.json()) as Order
})

export const deleteOrder = createAsyncThunk('orders/deleteOrder', async (id: number) => {
  const response = await fetch(`api/orders/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete order')
  return id
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
    builder.addCase(addOrder.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })
    builder.addCase(updateOrder.fulfilled, (state, action) => {
      const index = state.items.findIndex(o => o.id === action.payload.id)
      if (index !== -1) state.items[index] = action.payload
    })
    builder.addCase(deleteOrder.fulfilled, (state, action) => {
      state.items = state.items.filter(o => o.id !== action.payload)
    })
  }
})

export default ordersSlice.reducer

