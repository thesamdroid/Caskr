import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface StatusTask {
  id: number
  statusId: number
  name: string
}

export interface Status {
  id: number
  name: string
  statusTasks: StatusTask[]
}

export const fetchStatuses = createAsyncThunk('statuses/fetchStatuses', async () => {
  const response = await authorizedFetch('api/status')
  if (!response.ok) throw new Error('Failed to fetch statuses')
  return (await response.json()) as Status[]
})

export const addStatus = createAsyncThunk('statuses/addStatus', async (status: Omit<Status, 'id' | 'statusTasks'>) => {
  const response = await authorizedFetch('api/status', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(status)
  })
  if (!response.ok) throw new Error('Failed to add status')
  return (await response.json()) as Status
})

export const updateStatus = createAsyncThunk('statuses/updateStatus', async (status: Omit<Status, 'statusTasks'>) => {
  const response = await authorizedFetch(`api/status/${status.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(status)
  })
  if (!response.ok) throw new Error('Failed to update status')
  return (await response.json()) as Status
})

export const deleteStatus = createAsyncThunk('statuses/deleteStatus', async (id: number) => {
  const response = await authorizedFetch(`api/status/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete status')
  return id
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
    builder.addCase(addStatus.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })
    builder.addCase(updateStatus.fulfilled, (state, action) => {
      const index = state.items.findIndex(s => s.id === action.payload.id)
      if (index !== -1) state.items[index] = action.payload
    })
    builder.addCase(deleteStatus.fulfilled, (state, action) => {
      state.items = state.items.filter(s => s.id !== action.payload)
    })
  }
})

export default statusSlice.reducer

