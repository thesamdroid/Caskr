import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'

export interface User {
  id: number
  name: string
  email: string
  userTypeId: number
}

export const fetchUsers = createAsyncThunk('users/fetchUsers', async () => {
  const response = await fetch('api/users')
  if (!response.ok) throw new Error('Failed to fetch users')
  return (await response.json()) as User[]
})

export const addUser = createAsyncThunk('users/addUser', async (user: Omit<User, 'id'>) => {
  const response = await fetch('api/users', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(user)
  })
  if (!response.ok) throw new Error('Failed to add user')
  return (await response.json()) as User
})

export const updateUser = createAsyncThunk('users/updateUser', async (user: User) => {
  const response = await fetch(`api/users/${user.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(user)
  })
  if (!response.ok) throw new Error('Failed to update user')
  return (await response.json()) as User
})

export const deleteUser = createAsyncThunk('users/deleteUser', async (id: number) => {
  const response = await fetch(`api/users/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete user')
  return id
})

interface UsersState {
  items: User[]
}

const initialState: UsersState = { items: [] }

const usersSlice = createSlice({
  name: 'users',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchUsers.fulfilled, (state, action) => {
      state.items = action.payload
    })
    builder.addCase(addUser.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })
    builder.addCase(updateUser.fulfilled, (state, action) => {
      const index = state.items.findIndex(u => u.id === action.payload.id)
      if (index !== -1) state.items[index] = action.payload
    })
    builder.addCase(deleteUser.fulfilled, (state, action) => {
      state.items = state.items.filter(u => u.id !== action.payload)
    })
  }
})

export default usersSlice.reducer
