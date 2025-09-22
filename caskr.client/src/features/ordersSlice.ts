import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'
import type { StatusTask } from './statusSlice'

export interface Order {
  id: number
  name: string
  statusId: number
  ownerId: number
  spiritTypeId: number
  quantity: number
  mashBillId: number
}

export const fetchOrders = createAsyncThunk('orders/fetchOrders', async () => {
  const response = await authorizedFetch('api/orders')
  if (!response.ok) throw new Error('Failed to fetch orders')
  return (await response.json()) as Order[]
})

export const addOrder = createAsyncThunk('orders/addOrder', async (order: Omit<Order, 'id'>) => {
  const response = await authorizedFetch('api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  })
  if (!response.ok) throw new Error('Failed to add order')
  return (await response.json()) as Order
})

export const updateOrder = createAsyncThunk('orders/updateOrder', async (order: Order) => {
  const response = await authorizedFetch(`api/orders/${order.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order)
  })
  if (!response.ok) throw new Error('Failed to update order')
  return (await response.json()) as Order
})

export const deleteOrder = createAsyncThunk('orders/deleteOrder', async (id: number) => {
  const response = await authorizedFetch(`api/orders/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete order')
  return id
})

export const fetchOutstandingTasks = createAsyncThunk(
  'orders/fetchOutstandingTasks',
  async (orderId: number) => {
    const response = await authorizedFetch(`api/orders/${orderId}/outstanding-tasks`)
    if (!response.ok) throw new Error('Failed to fetch outstanding tasks')
    return { orderId, tasks: (await response.json()) as StatusTask[] }
  }
)

interface OrdersState {
  items: Order[]
  outstandingTasks: Record<number, StatusTask[]>
}

const initialState: OrdersState = { items: [], outstandingTasks: {} }

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
    builder.addCase(fetchOutstandingTasks.fulfilled, (state, action) => {
      state.outstandingTasks[action.payload.orderId] = action.payload.tasks
    })
  }
})

export default ordersSlice.reducer

