import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export type PurchaseOrderStatus = 'Draft' | 'Sent' | 'Confirmed' | 'Partial_Received' | 'Received' | 'Cancelled'
export type PaymentStatus = 'Unpaid' | 'Partial' | 'Paid'
export type ReceiptItemCondition = 'Good' | 'Damaged' | 'Partial'

export interface PurchaseOrderItem {
  id: number
  purchaseOrderId: number
  supplierProductId: number
  productName: string
  sku?: string
  unitOfMeasure?: string
  quantity: number
  unitPrice: number
  totalPrice: number
  receivedQuantity: number
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface PurchaseOrder {
  id: number
  companyId: number
  supplierId: number
  supplierName: string
  supplierEmail?: string
  poNumber: string
  orderDate: string
  expectedDeliveryDate?: string
  status: PurchaseOrderStatus
  totalAmount: number
  currency: string
  paymentStatus: PaymentStatus
  notes?: string
  createdByUserId?: number
  createdByUserName?: string
  createdAt: string
  updatedAt: string
  items?: PurchaseOrderItem[]
  lineItemCount?: number
  totalQuantityOrdered?: number
  totalQuantityReceived?: number
}

export interface PurchaseOrderItemRequest {
  supplierProductId: number
  quantity: number
  unitPrice: number
  notes?: string
}

export interface PurchaseOrderRequest {
  supplierId: number
  orderDate: string
  expectedDeliveryDate?: string
  notes?: string
  items: PurchaseOrderItemRequest[]
}

export interface InventoryReceiptItemRequest {
  purchaseOrderItemId: number
  receivedQuantity: number
  condition: ReceiptItemCondition
  notes?: string
}

export interface InventoryReceiptRequest {
  purchaseOrderId: number
  receiptDate: string
  notes?: string
  items: InventoryReceiptItemRequest[]
}

export interface InventoryReceipt {
  id: number
  purchaseOrderId: number
  receiptDate: string
  receivedByUserId?: number
  receivedByUserName?: string
  notes?: string
  createdAt: string
  items: InventoryReceiptItem[]
}

export interface InventoryReceiptItem {
  id: number
  inventoryReceiptId: number
  purchaseOrderItemId: number
  productName: string
  receivedQuantity: number
  condition: ReceiptItemCondition
  notes?: string
}

export interface SendPOEmailRequest {
  purchaseOrderId: number
  toEmail: string
  subject: string
  body: string
}

// Async Thunks
export const fetchPurchaseOrders = createAsyncThunk(
  'purchaseOrders/fetchPurchaseOrders',
  async ({
    status,
    supplierId,
    startDate,
    endDate
  }: {
    status?: PurchaseOrderStatus
    supplierId?: number
    startDate?: string
    endDate?: string
  } = {}) => {
    const params = new URLSearchParams()
    if (status) params.append('status', status)
    if (supplierId) params.append('supplierId', supplierId.toString())
    if (startDate) params.append('startDate', startDate)
    if (endDate) params.append('endDate', endDate)

    const url = `api/purchase-orders${params.toString() ? `?${params.toString()}` : ''}`
    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch purchase orders')
    return (await response.json()) as PurchaseOrder[]
  }
)

export const fetchPurchaseOrder = createAsyncThunk(
  'purchaseOrders/fetchPurchaseOrder',
  async (id: number) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}`)
    if (!response.ok) throw new Error('Failed to fetch purchase order')
    return (await response.json()) as PurchaseOrder
  }
)

export const getNextPONumber = createAsyncThunk(
  'purchaseOrders/getNextPONumber',
  async () => {
    const response = await authorizedFetch('api/purchase-orders/next-po-number')
    if (!response.ok) throw new Error('Failed to get next PO number')
    return (await response.json()) as { poNumber: string }
  }
)

export const createPurchaseOrder = createAsyncThunk(
  'purchaseOrders/createPurchaseOrder',
  async (po: PurchaseOrderRequest, { rejectWithValue }) => {
    const response = await authorizedFetch('api/purchase-orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(po)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to create purchase order' })
      }
    }

    return (await response.json()) as PurchaseOrder
  }
)

export const updatePurchaseOrder = createAsyncThunk(
  'purchaseOrders/updatePurchaseOrder',
  async ({ id, po }: { id: number; po: PurchaseOrderRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(po)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to update purchase order' })
      }
    }

    return (await response.json()) as PurchaseOrder
  }
)

export const sendPurchaseOrder = createAsyncThunk(
  'purchaseOrders/sendPurchaseOrder',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}/send`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to send purchase order' })
      }
    }

    return (await response.json()) as PurchaseOrder
  }
)

export const cancelPurchaseOrder = createAsyncThunk(
  'purchaseOrders/cancelPurchaseOrder',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}/cancel`, {
      method: 'POST'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to cancel purchase order' })
      }
    }

    return id
  }
)

export const deletePurchaseOrder = createAsyncThunk(
  'purchaseOrders/deletePurchaseOrder',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to delete purchase order' })
      }
    }

    return id
  }
)

// Receiving Thunks
export const createInventoryReceipt = createAsyncThunk(
  'purchaseOrders/createInventoryReceipt',
  async (receipt: InventoryReceiptRequest, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/inventory-receipts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(receipt)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to create inventory receipt' })
      }
    }

    return (await response.json()) as InventoryReceipt
  }
)

export const fetchInventoryReceipts = createAsyncThunk(
  'purchaseOrders/fetchInventoryReceipts',
  async (purchaseOrderId: number) => {
    const response = await authorizedFetch(`api/purchase-orders/${purchaseOrderId}/receipts`)
    if (!response.ok) throw new Error('Failed to fetch inventory receipts')
    return (await response.json()) as InventoryReceipt[]
  }
)

// Email Thunks
export const sendPOEmail = createAsyncThunk(
  'purchaseOrders/sendPOEmail',
  async (request: SendPOEmailRequest, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${request.purchaseOrderId}/email`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to send email' })
      }
    }

    return (await response.json()) as PurchaseOrder
  }
)

// PDF Thunks
export const generatePOPdf = createAsyncThunk(
  'purchaseOrders/generatePOPdf',
  async (id: number, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/purchase-orders/${id}/pdf`)

    if (!response.ok) {
      try {
        const error = await response.json()
        return rejectWithValue(error)
      } catch {
        return rejectWithValue({ message: 'Failed to generate PDF' })
      }
    }

    const blob = await response.blob()
    return URL.createObjectURL(blob)
  }
)

interface PurchaseOrdersState {
  items: PurchaseOrder[]
  currentPurchaseOrder: PurchaseOrder | null
  receipts: InventoryReceipt[]
  nextPONumber: string | null
  loading: boolean
  receiptsLoading: boolean
  pdfUrl: string | null
  error: string | null
}

const initialState: PurchaseOrdersState = {
  items: [],
  currentPurchaseOrder: null,
  receipts: [],
  nextPONumber: null,
  loading: false,
  receiptsLoading: false,
  pdfUrl: null,
  error: null
}

const purchaseOrdersSlice = createSlice({
  name: 'purchaseOrders',
  initialState,
  reducers: {
    clearPurchaseOrderError: state => {
      state.error = null
    },
    clearCurrentPurchaseOrder: state => {
      state.currentPurchaseOrder = null
      state.receipts = []
    },
    clearPdfUrl: state => {
      if (state.pdfUrl) {
        URL.revokeObjectURL(state.pdfUrl)
      }
      state.pdfUrl = null
    }
  },
  extraReducers: builder => {
    // fetchPurchaseOrders
    builder.addCase(fetchPurchaseOrders.pending, state => {
      state.loading = true
      state.error = null
    })
    builder.addCase(fetchPurchaseOrders.fulfilled, (state, action) => {
      state.items = action.payload
      state.loading = false
    })
    builder.addCase(fetchPurchaseOrders.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch purchase orders'
    })

    // fetchPurchaseOrder
    builder.addCase(fetchPurchaseOrder.pending, state => {
      state.loading = true
    })
    builder.addCase(fetchPurchaseOrder.fulfilled, (state, action) => {
      state.currentPurchaseOrder = action.payload
      state.loading = false
    })
    builder.addCase(fetchPurchaseOrder.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch purchase order'
    })

    // getNextPONumber
    builder.addCase(getNextPONumber.fulfilled, (state, action) => {
      state.nextPONumber = action.payload.poNumber
    })

    // createPurchaseOrder
    builder.addCase(createPurchaseOrder.fulfilled, (state, action) => {
      state.items.unshift(action.payload)
    })

    // updatePurchaseOrder
    builder.addCase(updatePurchaseOrder.fulfilled, (state, action) => {
      const index = state.items.findIndex(po => po.id === action.payload.id)
      if (index !== -1) {
        state.items[index] = action.payload
      }
      if (state.currentPurchaseOrder?.id === action.payload.id) {
        state.currentPurchaseOrder = action.payload
      }
    })

    // sendPurchaseOrder
    builder.addCase(sendPurchaseOrder.fulfilled, (state, action) => {
      const index = state.items.findIndex(po => po.id === action.payload.id)
      if (index !== -1) {
        state.items[index] = action.payload
      }
      if (state.currentPurchaseOrder?.id === action.payload.id) {
        state.currentPurchaseOrder = action.payload
      }
    })

    // cancelPurchaseOrder
    builder.addCase(cancelPurchaseOrder.fulfilled, (state, action) => {
      const index = state.items.findIndex(po => po.id === action.payload)
      if (index !== -1) {
        state.items[index].status = 'Cancelled'
      }
      if (state.currentPurchaseOrder?.id === action.payload) {
        state.currentPurchaseOrder.status = 'Cancelled'
      }
    })

    // deletePurchaseOrder
    builder.addCase(deletePurchaseOrder.fulfilled, (state, action) => {
      state.items = state.items.filter(po => po.id !== action.payload)
      if (state.currentPurchaseOrder?.id === action.payload) {
        state.currentPurchaseOrder = null
      }
    })

    // fetchInventoryReceipts
    builder.addCase(fetchInventoryReceipts.pending, state => {
      state.receiptsLoading = true
    })
    builder.addCase(fetchInventoryReceipts.fulfilled, (state, action) => {
      state.receipts = action.payload
      state.receiptsLoading = false
    })
    builder.addCase(fetchInventoryReceipts.rejected, state => {
      state.receiptsLoading = false
    })

    // createInventoryReceipt
    builder.addCase(createInventoryReceipt.fulfilled, (state, action) => {
      state.receipts.push(action.payload)
    })

    // sendPOEmail
    builder.addCase(sendPOEmail.fulfilled, (state, action) => {
      const index = state.items.findIndex(po => po.id === action.payload.id)
      if (index !== -1) {
        state.items[index] = action.payload
      }
      if (state.currentPurchaseOrder?.id === action.payload.id) {
        state.currentPurchaseOrder = action.payload
      }
    })

    // generatePOPdf
    builder.addCase(generatePOPdf.fulfilled, (state, action) => {
      if (state.pdfUrl) {
        URL.revokeObjectURL(state.pdfUrl)
      }
      state.pdfUrl = action.payload
    })
  }
})

export const { clearPurchaseOrderError, clearCurrentPurchaseOrder, clearPdfUrl } = purchaseOrdersSlice.actions
export default purchaseOrdersSlice.reducer
