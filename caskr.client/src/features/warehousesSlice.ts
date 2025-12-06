import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export type WarehouseType = 'Rickhouse' | 'Palletized' | 'Tank_Farm' | 'Outdoor'

export interface Warehouse {
  id: number
  companyId: number
  name: string
  warehouseType: WarehouseType
  addressLine1?: string
  addressLine2?: string
  city?: string
  state?: string
  postalCode?: string
  country?: string
  fullAddress: string
  totalCapacity: number
  lengthFeet?: number
  widthFeet?: number
  heightFeet?: number
  isActive: boolean
  notes?: string
  createdAt: string
  updatedAt: string
  createdByUserId?: number
  createdByUserName?: string
  occupiedPositions: number
  occupancyPercentage: number
  availablePositions: number
}

export interface WarehouseRequest {
  name: string
  warehouseType: WarehouseType
  addressLine1?: string
  addressLine2?: string
  city?: string
  state?: string
  postalCode?: string
  country?: string
  totalCapacity: number
  lengthFeet?: number
  widthFeet?: number
  heightFeet?: number
  notes?: string
}

export interface WarehouseCapacitySnapshot {
  id: number
  warehouseId: number
  snapshotDate: string
  totalCapacity: number
  occupiedPositions: number
  occupancyPercentage: number
}

// Async Thunks
export const fetchWarehouses = createAsyncThunk(
  'warehouses/fetchWarehouses',
  async ({ companyId, includeInactive = false }: { companyId: number; includeInactive?: boolean }) => {
    const response = await authorizedFetch(
      `api/warehouses/company/${companyId}?includeInactive=${includeInactive}`
    )
    if (!response.ok) throw new Error('Failed to fetch warehouses')
    return (await response.json()) as Warehouse[]
  }
)

export const fetchWarehouse = createAsyncThunk(
  'warehouses/fetchWarehouse',
  async (id: number) => {
    const response = await authorizedFetch(`api/warehouses/${id}`)
    if (!response.ok) throw new Error('Failed to fetch warehouse')
    return (await response.json()) as Warehouse
  }
)

export const createWarehouse = createAsyncThunk(
  'warehouses/createWarehouse',
  async ({ companyId, warehouse }: { companyId: number; warehouse: WarehouseRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/warehouses/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(warehouse)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to create warehouse' })
      }
    }

    return (await response.json()) as Warehouse
  }
)

export const updateWarehouse = createAsyncThunk(
  'warehouses/updateWarehouse',
  async ({ id, warehouse }: { id: number; warehouse: WarehouseRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/warehouses/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(warehouse)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to update warehouse' })
      }
    }

    return (await response.json()) as Warehouse
  }
)

export const deactivateWarehouse = createAsyncThunk(
  'warehouses/deactivateWarehouse',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/warehouses/${id}/deactivate`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to deactivate warehouse' })
      }
    }

    return id
  }
)

export const activateWarehouse = createAsyncThunk(
  'warehouses/activateWarehouse',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/warehouses/${id}/activate`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to activate warehouse' })
      }
    }

    return id
  }
)

export const fetchCapacityHistory = createAsyncThunk(
  'warehouses/fetchCapacityHistory',
  async ({
    warehouseId,
    startDate,
    endDate
  }: {
    warehouseId: number
    startDate?: string
    endDate?: string
  }) => {
    let url = `api/warehouses/${warehouseId}/capacity-history`
    const params = new URLSearchParams()
    if (startDate) params.append('startDate', startDate)
    if (endDate) params.append('endDate', endDate)
    if (params.toString()) url += `?${params.toString()}`

    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch capacity history')
    return (await response.json()) as WarehouseCapacitySnapshot[]
  }
)

interface WarehousesState {
  items: Warehouse[]
  selectedWarehouseId: number | null // null means "All Warehouses"
  currentWarehouse: Warehouse | null
  capacityHistory: WarehouseCapacitySnapshot[]
  loading: boolean
  error: string | null
}

const initialState: WarehousesState = {
  items: [],
  selectedWarehouseId: null,
  currentWarehouse: null,
  capacityHistory: [],
  loading: false,
  error: null
}

const warehousesSlice = createSlice({
  name: 'warehouses',
  initialState,
  reducers: {
    setSelectedWarehouseId: (state, action: PayloadAction<number | null>) => {
      state.selectedWarehouseId = action.payload
    },
    clearWarehouseError: state => {
      state.error = null
    },
    resetWarehousesState: () => initialState
  },
  extraReducers: builder => {
    // fetchWarehouses
    builder.addCase(fetchWarehouses.pending, state => {
      state.loading = true
      state.error = null
    })
    builder.addCase(fetchWarehouses.fulfilled, (state, action) => {
      state.items = action.payload
      state.loading = false
    })
    builder.addCase(fetchWarehouses.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch warehouses'
    })

    // fetchWarehouse
    builder.addCase(fetchWarehouse.fulfilled, (state, action) => {
      state.currentWarehouse = action.payload
    })

    // createWarehouse
    builder.addCase(createWarehouse.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })

    // updateWarehouse
    builder.addCase(updateWarehouse.fulfilled, (state, action) => {
      const index = state.items.findIndex(w => w.id === action.payload.id)
      if (index !== -1) {
        state.items[index] = action.payload
      }
      if (state.currentWarehouse?.id === action.payload.id) {
        state.currentWarehouse = action.payload
      }
    })

    // deactivateWarehouse
    builder.addCase(deactivateWarehouse.fulfilled, (state, action) => {
      const index = state.items.findIndex(w => w.id === action.payload)
      if (index !== -1) {
        state.items[index].isActive = false
      }
    })

    // activateWarehouse
    builder.addCase(activateWarehouse.fulfilled, (state, action) => {
      const index = state.items.findIndex(w => w.id === action.payload)
      if (index !== -1) {
        state.items[index].isActive = true
      }
    })

    // fetchCapacityHistory
    builder.addCase(fetchCapacityHistory.fulfilled, (state, action) => {
      state.capacityHistory = action.payload
    })
  }
})

export const { setSelectedWarehouseId, clearWarehouseError, resetWarehousesState } = warehousesSlice.actions
export default warehousesSlice.reducer
