import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export interface Product {
  id: number
  ownerId: number
  notes?: string | null
}

export const fetchProducts = createAsyncThunk('products/fetchProducts', async () => {
  const response = await authorizedFetch('api/products')
  if (!response.ok) throw new Error('Failed to fetch products')
  return (await response.json()) as Product[]
})

export const addProduct = createAsyncThunk('products/addProduct', async (product: Omit<Product, 'id'>) => {
  const response = await authorizedFetch('api/products', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(product)
  })
  if (!response.ok) throw new Error('Failed to add product')
  return (await response.json()) as Product
})

export const updateProduct = createAsyncThunk('products/updateProduct', async (product: Product) => {
  const response = await authorizedFetch(`api/products/${product.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(product)
  })
  if (!response.ok) throw new Error('Failed to update product')
  return (await response.json()) as Product
})

export const deleteProduct = createAsyncThunk('products/deleteProduct', async (id: number) => {
  const response = await authorizedFetch(`api/products/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error('Failed to delete product')
  return id
})

interface ProductsState {
  items: Product[]
}

const initialState: ProductsState = { items: [] }

const productsSlice = createSlice({
  name: 'products',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder.addCase(fetchProducts.fulfilled, (state, action) => {
      state.items = action.payload
    })
    builder.addCase(addProduct.fulfilled, (state, action) => {
      state.items.push(action.payload)
    })
    builder.addCase(updateProduct.fulfilled, (state, action) => {
      const index = state.items.findIndex(p => p.id === action.payload.id)
      if (index !== -1) state.items[index] = action.payload
    })
    builder.addCase(deleteProduct.fulfilled, (state, action) => {
      state.items = state.items.filter(p => p.id !== action.payload)
    })
  }
})

export default productsSlice.reducer
