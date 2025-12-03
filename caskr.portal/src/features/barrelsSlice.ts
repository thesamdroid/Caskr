import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { CaskOwnership } from '../types/portal'
import { barrelsApi } from '../api/portalApi'

interface BarrelsState {
  ownerships: CaskOwnership[]
  selectedOwnership: CaskOwnership | null
  isLoading: boolean
  error: string | null
}

const initialState: BarrelsState = {
  ownerships: [],
  selectedOwnership: null,
  isLoading: false,
  error: null
}

export const fetchMyBarrels = createAsyncThunk<
  CaskOwnership[],
  void,
  { rejectValue: string }
>('barrels/fetchMyBarrels', async (_, { rejectWithValue }) => {
  try {
    const response = await barrelsApi.getMyBarrels()
    return response as unknown as CaskOwnership[]
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch barrels')
  }
})

export const fetchBarrelDetail = createAsyncThunk<
  CaskOwnership,
  number,
  { rejectValue: string }
>('barrels/fetchBarrelDetail', async (id, { rejectWithValue }) => {
  try {
    const response = await barrelsApi.getBarrelDetail(id)
    return response as unknown as CaskOwnership
  } catch (error) {
    return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch barrel details')
  }
})

const barrelsSlice = createSlice({
  name: 'barrels',
  initialState,
  reducers: {
    clearSelectedOwnership: state => {
      state.selectedOwnership = null
    },
    clearError: state => {
      state.error = null
    }
  },
  extraReducers: builder => {
    builder
      // Fetch my barrels
      .addCase(fetchMyBarrels.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchMyBarrels.fulfilled, (state, action) => {
        state.ownerships = action.payload
        state.isLoading = false
      })
      .addCase(fetchMyBarrels.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Failed to fetch barrels'
      })
      // Fetch barrel detail
      .addCase(fetchBarrelDetail.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchBarrelDetail.fulfilled, (state, action) => {
        state.selectedOwnership = action.payload
        state.isLoading = false
      })
      .addCase(fetchBarrelDetail.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload ?? 'Failed to fetch barrel details'
      })
  }
})

export const { clearSelectedOwnership, clearError } = barrelsSlice.actions
export default barrelsSlice.reducer
