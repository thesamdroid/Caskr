import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export type SupplierType = 'Grain' | 'Cooperage' | 'Bottles' | 'Labels' | 'Chemicals' | 'Equipment' | 'Other'

export interface Supplier {
  id: number
  companyId: number
  supplierName: string
  supplierType: SupplierType
  contactPerson?: string
  email?: string
  phone?: string
  address?: string
  website?: string
  paymentTerms?: string
  isActive: boolean
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface SupplierProduct {
  id: number
  supplierId: number
  productName: string
  productCategory?: string
  sku?: string
  unitOfMeasure?: string
  currentPrice?: number
  currency: string
  leadTimeDays?: number
  minimumOrderQuantity?: number
  notes?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface SupplierRequest {
  supplierName: string
  supplierType: SupplierType
  contactPerson?: string
  email?: string
  phone?: string
  address?: string
  website?: string
  paymentTerms?: string
  notes?: string
}

export interface SupplierProductRequest {
  productName: string
  productCategory?: string
  sku?: string
  unitOfMeasure?: string
  currentPrice?: number
  currency?: string
  leadTimeDays?: number
  minimumOrderQuantity?: number
  notes?: string
}

// Async Thunks
export const fetchSuppliers = createAsyncThunk(
  'suppliers/fetchSuppliers',
  async ({ includeInactive = false }: { includeInactive?: boolean } = {}) => {
    const response = await authorizedFetch(
      `api/suppliers?includeInactive=${includeInactive}`
    )
    if (!response.ok) throw new Error('Failed to fetch suppliers')
    return (await response.json()) as Supplier[]
  }
)

export const fetchSupplier = createAsyncThunk(
  'suppliers/fetchSupplier',
  async (id: number) => {
    const response = await authorizedFetch(`api/suppliers/${id}`)
    if (!response.ok) throw new Error('Failed to fetch supplier')
    return (await response.json()) as Supplier
  }
)

export const createSupplier = createAsyncThunk(
  'suppliers/createSupplier',
  async (supplier: SupplierRequest, { rejectWithValue }) => {
    const response = await authorizedFetch('api/suppliers', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(supplier)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to create supplier' })
      }
    }

    return (await response.json()) as Supplier
  }
)

export const updateSupplier = createAsyncThunk(
  'suppliers/updateSupplier',
  async ({ id, supplier }: { id: number; supplier: SupplierRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/suppliers/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(supplier)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to update supplier' })
      }
    }

    return (await response.json()) as Supplier
  }
)

export const deactivateSupplier = createAsyncThunk(
  'suppliers/deactivateSupplier',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/suppliers/${id}/deactivate`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to deactivate supplier' })
      }
    }

    return id
  }
)

export const activateSupplier = createAsyncThunk(
  'suppliers/activateSupplier',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/suppliers/${id}/activate`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to activate supplier' })
      }
    }

    return id
  }
)

// Supplier Products Thunks
export const fetchSupplierProducts = createAsyncThunk(
  'suppliers/fetchSupplierProducts',
  async ({ supplierId, includeInactive = false }: { supplierId: number; includeInactive?: boolean }) => {
    const response = await authorizedFetch(
      `api/suppliers/${supplierId}/products?includeInactive=${includeInactive}`
    )
    if (!response.ok) throw new Error('Failed to fetch supplier products')
    return (await response.json()) as SupplierProduct[]
  }
)

export const createSupplierProduct = createAsyncThunk(
  'suppliers/createSupplierProduct',
  async ({ supplierId, product }: { supplierId: number; product: SupplierProductRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/suppliers/${supplierId}/products`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(product)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to create supplier product' })
      }
    }

    return (await response.json()) as SupplierProduct
  }
)

export const updateSupplierProduct = createAsyncThunk(
  'suppliers/updateSupplierProduct',
  async ({ id, product }: { id: number; product: SupplierProductRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/supplier-products/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(product)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to update supplier product' })
      }
    }

    return (await response.json()) as SupplierProduct
  }
)

interface SuppliersState {
  items: Supplier[]
  currentSupplier: Supplier | null
  supplierProducts: SupplierProduct[]
  loading: boolean
  productsLoading: boolean
  error: string | null
}

const initialState: SuppliersState = {
  items: [],
  currentSupplier: null,
  supplierProducts: [],
  loading: false,
  productsLoading: false,
  error: null
}

const suppliersSlice = createSlice({
  name: 'suppliers',
  initialState,
  reducers: {
    clearSupplierError: state => {
      state.error = null
    },
    clearSupplierProducts: state => {
      state.supplierProducts = []
    }
  },
  extraReducers: builder => {
    // fetchSuppliers
    builder.addCase(fetchSuppliers.pending, state => {
      state.loading = true
      state.error = null
    })
    builder.addCase(fetchSuppliers.fulfilled, (state, action) => {
      state.items = action.payload
      state.loading = false
    })
    builder.addCase(fetchSuppliers.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch suppliers'
    })

    // fetchSupplier
    builder.addCase(fetchSupplier.fulfilled, (state, action) => {
      state.currentSupplier = action.payload
    })

    // createSupplier
    builder.addCase(createSupplier.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })

    // updateSupplier
    builder.addCase(updateSupplier.fulfilled, (state, action) => {
      const index = state.items.findIndex(s => s.id === action.payload.id)
      if (index !== -1) {
        state.items[index] = action.payload
      }
      if (state.currentSupplier?.id === action.payload.id) {
        state.currentSupplier = action.payload
      }
    })

    // deactivateSupplier
    builder.addCase(deactivateSupplier.fulfilled, (state, action) => {
      const index = state.items.findIndex(s => s.id === action.payload)
      if (index !== -1) {
        state.items[index].isActive = false
      }
    })

    // activateSupplier
    builder.addCase(activateSupplier.fulfilled, (state, action) => {
      const index = state.items.findIndex(s => s.id === action.payload)
      if (index !== -1) {
        state.items[index].isActive = true
      }
    })

    // fetchSupplierProducts
    builder.addCase(fetchSupplierProducts.pending, state => {
      state.productsLoading = true
    })
    builder.addCase(fetchSupplierProducts.fulfilled, (state, action) => {
      state.supplierProducts = action.payload
      state.productsLoading = false
    })
    builder.addCase(fetchSupplierProducts.rejected, state => {
      state.productsLoading = false
    })

    // createSupplierProduct
    builder.addCase(createSupplierProduct.fulfilled, (state, action) => {
      state.supplierProducts.push(action.payload)
    })

    // updateSupplierProduct
    builder.addCase(updateSupplierProduct.fulfilled, (state, action) => {
      const index = state.supplierProducts.findIndex(p => p.id === action.payload.id)
      if (index !== -1) {
        state.supplierProducts[index] = action.payload
      }
    })
  }
})

export const { clearSupplierError, clearSupplierProducts } = suppliersSlice.actions
export default suppliersSlice.reducer
