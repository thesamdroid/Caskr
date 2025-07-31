import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'

export interface UserType {
  id: number
  name: string | null
}

export const fetchUserTypes = createAsyncThunk('userTypes/fetchUserTypes', async () => {
  const response = await fetch('api/usertypes')
  if (!response.ok) throw new Error('Failed to fetch user types')
  return (await response.json()) as UserType[]
})

export const addUserType = createAsyncThunk('userTypes/addUserType', async (userType: Omit<UserType, 'id'>) => {
  const response = await fetch('api/usertypes', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(userType)
  })
  if (!response.ok) throw new Error('Failed to add user type')
  return (await response.json()) as UserType
})

export const updateUserType = createAsyncThunk('userTypes/updateUserType', async (userType: UserType) => {
  const response = await fetch(`api/usertypes/${userType.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(userType)
  })
  if (!response.ok) throw new Error('Failed to update user type')
  return (await response.json()) as UserType
})

export const deleteUserType = createAsyncThunk('userTypes/deleteUserType', async (id: number) => {
  const response = await fetch(`api/usertypes/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete user type')
  return id
})

interface UserTypesState {
  items: UserType[]
}

const initialState: UserTypesState = { items: [] }

const userTypesSlice = createSlice({
  name: 'userTypes',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchUserTypes.fulfilled, (state, action) => {
      state.items = action.payload
    })
    builder.addCase(addUserType.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })
    builder.addCase(updateUserType.fulfilled, (state, action) => {
      const index = state.items.findIndex(u => u.id === action.payload.id)
      if (index !== -1) state.items[index] = action.payload
    })
    builder.addCase(deleteUserType.fulfilled, (state, action) => {
      state.items = state.items.filter(u => u.id !== action.payload)
    })
  }
})

export default userTypesSlice.reducer
