import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  createPurchaseOrder,
  updatePurchaseOrder,
  fetchPurchaseOrders,
  fetchPurchaseOrder,
  getNextPONumber,
  PurchaseOrder,
  PurchaseOrderItemRequest
} from '../features/purchaseOrdersSlice'
import {
  fetchSuppliers,
  fetchSupplierProducts,
  clearSupplierProducts
} from '../features/suppliersSlice'

interface LineItem {
  id: string
  supplierProductId: number
  productName: string
  sku?: string
  unitOfMeasure?: string
  quantity: number
  unitPrice: number
  notes?: string
}

interface PurchaseOrderFormModalProps {
  isOpen: boolean
  onClose: () => void
  purchaseOrder: PurchaseOrder | null
}

function generateId(): string {
  return Math.random().toString(36).substr(2, 9)
}

function PurchaseOrderFormModal({
  isOpen,
  onClose,
  purchaseOrder
}: PurchaseOrderFormModalProps) {
  const dispatch = useAppDispatch()
  const suppliers = useAppSelector(state => state.suppliers.items)
  const supplierProducts = useAppSelector(state => state.suppliers.supplierProducts)
  const productsLoading = useAppSelector(state => state.suppliers.productsLoading)
  const nextPONumber = useAppSelector(state => state.purchaseOrders.nextPONumber)

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form state
  const [supplierId, setSupplierId] = useState<number | ''>('')
  const [orderDate, setOrderDate] = useState('')
  const [expectedDeliveryDate, setExpectedDeliveryDate] = useState('')
  const [notes, setNotes] = useState('')
  const [lineItems, setLineItems] = useState<LineItem[]>([])

  // For adding new items
  const [selectedProductId, setSelectedProductId] = useState<number | ''>('')

  // Fetch suppliers on mount
  useEffect(() => {
    dispatch(fetchSuppliers({ includeInactive: false }))
  }, [dispatch])

  // Get next PO number for new orders
  useEffect(() => {
    if (isOpen && !purchaseOrder) {
      dispatch(getNextPONumber())
    }
  }, [dispatch, isOpen, purchaseOrder])

  // Fetch supplier products when supplier changes
  useEffect(() => {
    if (supplierId) {
      dispatch(fetchSupplierProducts({ supplierId, includeInactive: false }))
    } else {
      dispatch(clearSupplierProducts())
    }
  }, [dispatch, supplierId])

  // Initialize form when modal opens or PO changes
  useEffect(() => {
    if (isOpen) {
      if (purchaseOrder) {
        // Editing existing PO - fetch full details
        dispatch(fetchPurchaseOrder(purchaseOrder.id)).then((action) => {
          if (fetchPurchaseOrder.fulfilled.match(action)) {
            const po = action.payload
            setSupplierId(po.supplierId)
            setOrderDate(po.orderDate.split('T')[0])
            setExpectedDeliveryDate(po.expectedDeliveryDate?.split('T')[0] || '')
            setNotes(po.notes || '')

            if (po.items) {
              setLineItems(po.items.map(item => ({
                id: generateId(),
                supplierProductId: item.supplierProductId,
                productName: item.productName,
                sku: item.sku,
                unitOfMeasure: item.unitOfMeasure,
                quantity: item.quantity,
                unitPrice: item.unitPrice,
                notes: item.notes
              })))
            }
          }
        })
      } else {
        // Creating new PO
        setSupplierId('')
        setOrderDate(new Date().toISOString().split('T')[0])
        setExpectedDeliveryDate('')
        setNotes('')
        setLineItems([])
      }
      setError(null)
      setSelectedProductId('')
    }
  }, [dispatch, purchaseOrder, isOpen])

  const handleAddItem = () => {
    if (!selectedProductId) return

    const product = supplierProducts.find(p => p.id === selectedProductId)
    if (!product) return

    // Check if product already added
    if (lineItems.some(item => item.supplierProductId === product.id)) {
      setError('This product is already in the order')
      return
    }

    setLineItems([...lineItems, {
      id: generateId(),
      supplierProductId: product.id,
      productName: product.productName,
      sku: product.sku,
      unitOfMeasure: product.unitOfMeasure,
      quantity: product.minimumOrderQuantity || 1,
      unitPrice: product.currentPrice || 0,
      notes: ''
    }])

    setSelectedProductId('')
    setError(null)
  }

  const handleRemoveItem = (itemId: string) => {
    setLineItems(lineItems.filter(item => item.id !== itemId))
  }

  const handleItemChange = (itemId: string, field: keyof LineItem, value: number | string) => {
    setLineItems(lineItems.map(item => {
      if (item.id === itemId) {
        return { ...item, [field]: value }
      }
      return item
    }))
  }

  const calculateLineTotal = (item: LineItem): number => {
    return item.quantity * item.unitPrice
  }

  const calculateGrandTotal = (): number => {
    return lineItems.reduce((sum, item) => sum + calculateLineTotal(item), 0)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validation
    if (!supplierId) {
      setError('Please select a supplier')
      return
    }

    if (!orderDate) {
      setError('Order date is required')
      return
    }

    if (lineItems.length === 0) {
      setError('Please add at least one line item')
      return
    }

    const invalidItems = lineItems.filter(item => item.quantity <= 0 || item.unitPrice < 0)
    if (invalidItems.length > 0) {
      setError('Please ensure all quantities are positive and prices are non-negative')
      return
    }

    const items: PurchaseOrderItemRequest[] = lineItems.map(item => ({
      supplierProductId: item.supplierProductId,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      notes: item.notes
    }))

    setSaving(true)

    try {
      if (purchaseOrder) {
        await dispatch(updatePurchaseOrder({
          id: purchaseOrder.id,
          po: {
            supplierId,
            orderDate,
            expectedDeliveryDate: expectedDeliveryDate || undefined,
            notes: notes || undefined,
            items
          }
        })).unwrap()
      } else {
        await dispatch(createPurchaseOrder({
          supplierId,
          orderDate,
          expectedDeliveryDate: expectedDeliveryDate || undefined,
          notes: notes || undefined,
          items
        })).unwrap()
      }

      dispatch(fetchPurchaseOrders({}))
      onClose()
    } catch (err: unknown) {
      const errorMessage = err && typeof err === 'object' && 'message' in err
        ? (err as { message: string }).message
        : 'Failed to save purchase order'
      setError(errorMessage)
    } finally {
      setSaving(false)
    }
  }

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount)
  }

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-xlarge" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{purchaseOrder ? `Edit PO: ${purchaseOrder.poNumber}` : `New Purchase Order${nextPONumber ? `: ${nextPONumber}` : ''}`}</h2>
          <button onClick={onClose} className="modal-close" aria-label="Close">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="form-error">{error}</div>
            )}

            <div className="form-row">
              <div className="form-group form-group-large">
                <label htmlFor="supplier">Supplier *</label>
                <select
                  id="supplier"
                  value={supplierId}
                  onChange={e => setSupplierId(e.target.value ? parseInt(e.target.value, 10) : '')}
                  required
                  disabled={!!purchaseOrder}
                >
                  <option value="">Select a supplier...</option>
                  {suppliers.map(s => (
                    <option key={s.id} value={s.id}>{s.supplierName}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="order-date">Order Date *</label>
                <input
                  id="order-date"
                  type="date"
                  value={orderDate}
                  onChange={e => setOrderDate(e.target.value)}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="delivery-date">Expected Delivery Date</label>
                <input
                  id="delivery-date"
                  type="date"
                  value={expectedDeliveryDate}
                  onChange={e => setExpectedDeliveryDate(e.target.value)}
                  min={orderDate}
                />
              </div>
            </div>

            {/* Line Items Section */}
            <fieldset className="form-fieldset">
              <legend>Line Items</legend>

              {supplierId ? (
                <>
                  {/* Add Item Row */}
                  <div className="add-item-row">
                    <select
                      value={selectedProductId}
                      onChange={e => setSelectedProductId(e.target.value ? parseInt(e.target.value, 10) : '')}
                      disabled={productsLoading}
                      className="product-select"
                    >
                      <option value="">
                        {productsLoading ? 'Loading products...' : 'Select a product to add...'}
                      </option>
                      {supplierProducts
                        .filter(p => !lineItems.some(item => item.supplierProductId === p.id))
                        .map(p => (
                          <option key={p.id} value={p.id}>
                            {p.productName} {p.sku ? `(${p.sku})` : ''} - {formatCurrency(p.currentPrice || 0)}
                          </option>
                        ))}
                    </select>
                    <button
                      type="button"
                      onClick={handleAddItem}
                      className="button-primary"
                      disabled={!selectedProductId}
                    >
                      + Add Item
                    </button>
                  </div>

                  {lineItems.length === 0 ? (
                    <div className="empty-items">
                      <p>No items added yet. Select a product above to add it to the order.</p>
                    </div>
                  ) : (
                    <div className="line-items-table-container">
                      <table className="line-items-table">
                        <thead>
                          <tr>
                            <th>Product</th>
                            <th>Quantity</th>
                            <th>Unit</th>
                            <th>Unit Price</th>
                            <th>Total</th>
                            <th></th>
                          </tr>
                        </thead>
                        <tbody>
                          {lineItems.map(item => (
                            <tr key={item.id}>
                              <td>
                                <div className="product-info">
                                  <span className="product-name">{item.productName}</span>
                                  {item.sku && <span className="product-sku">{item.sku}</span>}
                                </div>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0.01"
                                  step="0.01"
                                  value={item.quantity}
                                  onChange={e => handleItemChange(item.id, 'quantity', parseFloat(e.target.value) || 0)}
                                  className="quantity-input"
                                />
                              </td>
                              <td>
                                <span className="text-secondary">{item.unitOfMeasure || 'each'}</span>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0"
                                  step="0.01"
                                  value={item.unitPrice}
                                  onChange={e => handleItemChange(item.id, 'unitPrice', parseFloat(e.target.value) || 0)}
                                  className="price-input"
                                />
                              </td>
                              <td>
                                <span className="line-total">{formatCurrency(calculateLineTotal(item))}</span>
                              </td>
                              <td>
                                <button
                                  type="button"
                                  onClick={() => handleRemoveItem(item.id)}
                                  className="remove-button"
                                  title="Remove item"
                                >
                                  &times;
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                        <tfoot>
                          <tr>
                            <td colSpan={4} className="grand-total-label">Grand Total</td>
                            <td className="grand-total-value">{formatCurrency(calculateGrandTotal())}</td>
                            <td></td>
                          </tr>
                        </tfoot>
                      </table>
                    </div>
                  )}
                </>
              ) : (
                <div className="empty-items">
                  <p>Please select a supplier first to add products.</p>
                </div>
              )}
            </fieldset>

            <div className="form-group">
              <label htmlFor="notes">Notes</label>
              <textarea
                id="notes"
                value={notes}
                onChange={e => setNotes(e.target.value)}
                rows={3}
                placeholder="Additional notes or special instructions..."
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="button-secondary" disabled={saving}>
              Cancel
            </button>
            <button type="submit" className="button-primary" disabled={saving}>
              {saving ? 'Saving...' : purchaseOrder ? 'Update PO' : 'Save as Draft'}
            </button>
          </div>
        </form>

        <style>{`
          .modal-xlarge {
            max-width: 900px;
          }

          .form-error {
            background: var(--color-error-bg, rgba(239, 68, 68, 0.1));
            color: var(--color-error);
            padding: 0.75rem;
            border-radius: 4px;
            margin-bottom: 1rem;
          }

          .form-fieldset {
            border: 1px solid var(--color-border);
            border-radius: 4px;
            padding: 1rem;
            margin-bottom: 1rem;
          }

          .form-fieldset legend {
            padding: 0 0.5rem;
            font-weight: 500;
            color: var(--color-text-secondary);
          }

          .form-row {
            display: flex;
            gap: 1rem;
          }

          .form-group {
            flex: 1;
            margin-bottom: 1rem;
          }

          .form-group-large {
            flex: 2;
          }

          .form-group label {
            display: block;
            margin-bottom: 0.25rem;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
          }

          .form-group input,
          .form-group select,
          .form-group textarea {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
            font-size: 1rem;
          }

          .form-group input:focus,
          .form-group select:focus,
          .form-group textarea:focus {
            outline: none;
            border-color: var(--gold-primary, #D4AF37);
          }

          .form-group textarea {
            resize: vertical;
          }

          .add-item-row {
            display: flex;
            gap: 0.5rem;
            margin-bottom: 1rem;
          }

          .product-select {
            flex: 1;
            padding: 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
          }

          .empty-items {
            text-align: center;
            padding: 2rem;
            color: var(--color-text-secondary);
            background: var(--color-surface);
            border-radius: 4px;
          }

          .line-items-table-container {
            overflow-x: auto;
          }

          .line-items-table {
            width: 100%;
            border-collapse: collapse;
          }

          .line-items-table th,
          .line-items-table td {
            padding: 0.5rem;
            text-align: left;
            border-bottom: 1px solid var(--color-border);
          }

          .line-items-table th {
            font-size: 0.75rem;
            text-transform: uppercase;
            color: var(--color-text-secondary);
          }

          .product-info {
            display: flex;
            flex-direction: column;
          }

          .product-name {
            font-weight: 500;
          }

          .product-sku {
            font-size: 0.75rem;
            color: var(--color-text-secondary);
          }

          .quantity-input,
          .price-input {
            width: 80px;
            padding: 0.25rem 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
          }

          .line-total {
            font-weight: 500;
          }

          .remove-button {
            background: none;
            border: none;
            color: var(--color-error);
            font-size: 1.25rem;
            cursor: pointer;
            padding: 0 0.5rem;
          }

          .remove-button:hover {
            opacity: 0.8;
          }

          .grand-total-label {
            text-align: right;
            font-weight: 600;
          }

          .grand-total-value {
            font-weight: 600;
            font-size: 1.125rem;
            color: var(--gold-primary, #D4AF37);
          }

          .modal-overlay {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1000;
          }

          .modal {
            background: var(--color-background);
            border-radius: 8px;
            width: 90%;
            max-height: 90vh;
            overflow-y: auto;
          }

          .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            border-bottom: 1px solid var(--color-border);
          }

          .modal-header h2 {
            margin: 0;
            font-size: 1.25rem;
          }

          .modal-close {
            background: none;
            border: none;
            font-size: 1.5rem;
            cursor: pointer;
            color: var(--color-text-secondary);
          }

          .modal-body {
            padding: 1rem;
          }

          .modal-footer {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
            padding: 1rem;
            border-top: 1px solid var(--color-border);
          }

          .button-primary {
            background: var(--gold-primary, #D4AF37);
            color: var(--color-background);
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-primary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }

          .button-secondary {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            color: var(--color-text);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-secondary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }
        `}</style>
      </div>
    </div>
  )
}

export default PurchaseOrderFormModal
