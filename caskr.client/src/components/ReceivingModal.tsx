import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchPurchaseOrder,
  createInventoryReceipt,
  PurchaseOrder,
  ReceiptItemCondition,
  InventoryReceiptItemRequest
} from '../features/purchaseOrdersSlice'

interface ReceiveItem {
  purchaseOrderItemId: number
  productName: string
  sku?: string
  orderedQuantity: number
  previouslyReceived: number
  remainingQuantity: number
  receivedQuantity: number
  condition: ReceiptItemCondition
  notes: string
}

interface ReceivingModalProps {
  isOpen: boolean
  onClose: () => void
  purchaseOrder: PurchaseOrder
}

const CONDITION_OPTIONS: { value: ReceiptItemCondition; label: string }[] = [
  { value: 'Good', label: 'Good' },
  { value: 'Damaged', label: 'Damaged' },
  { value: 'Partial', label: 'Partial' }
]

function ReceivingModal({ isOpen, onClose, purchaseOrder }: ReceivingModalProps) {
  const dispatch = useAppDispatch()
  const currentPO = useAppSelector(state => state.purchaseOrders.currentPurchaseOrder)
  const loading = useAppSelector(state => state.purchaseOrders.loading)

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [receiptDate, setReceiptDate] = useState(new Date().toISOString().split('T')[0])
  const [receiptNotes, setReceiptNotes] = useState('')
  const [receiveItems, setReceiveItems] = useState<ReceiveItem[]>([])

  // Fetch full PO details
  useEffect(() => {
    if (isOpen && purchaseOrder) {
      dispatch(fetchPurchaseOrder(purchaseOrder.id))
    }
  }, [dispatch, isOpen, purchaseOrder])

  // Initialize receive items when PO data is loaded
  useEffect(() => {
    if (currentPO?.items) {
      setReceiveItems(currentPO.items.map(item => ({
        purchaseOrderItemId: item.id,
        productName: item.productName,
        sku: item.sku,
        orderedQuantity: item.quantity,
        previouslyReceived: item.receivedQuantity,
        remainingQuantity: item.quantity - item.receivedQuantity,
        receivedQuantity: item.quantity - item.receivedQuantity, // Default to remaining
        condition: 'Good',
        notes: ''
      })))
    }
  }, [currentPO])

  const handleItemChange = (
    itemId: number,
    field: 'receivedQuantity' | 'condition' | 'notes',
    value: number | string
  ) => {
    setReceiveItems(items => items.map(item => {
      if (item.purchaseOrderItemId === itemId) {
        return { ...item, [field]: value }
      }
      return item
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validate that at least one item has a received quantity
    const itemsToReceive = receiveItems.filter(item => item.receivedQuantity > 0)
    if (itemsToReceive.length === 0) {
      setError('Please enter a received quantity for at least one item')
      return
    }

    // Validate quantities
    const invalidItems = itemsToReceive.filter(
      item => item.receivedQuantity < 0 || item.receivedQuantity > item.remainingQuantity
    )
    if (invalidItems.length > 0) {
      setError('Received quantity cannot be negative or exceed remaining quantity')
      return
    }

    const receiptItems: InventoryReceiptItemRequest[] = itemsToReceive.map(item => ({
      purchaseOrderItemId: item.purchaseOrderItemId,
      receivedQuantity: item.receivedQuantity,
      condition: item.condition,
      notes: item.notes || undefined
    }))

    setSaving(true)

    try {
      await dispatch(createInventoryReceipt({
        purchaseOrderId: purchaseOrder.id,
        receiptDate,
        notes: receiptNotes || undefined,
        items: receiptItems
      })).unwrap()

      onClose()
    } catch (err: unknown) {
      const errorMessage = err && typeof err === 'object' && 'message' in err
        ? (err as { message: string }).message
        : 'Failed to create receipt'
      setError(errorMessage)
    } finally {
      setSaving(false)
    }
  }

  const totalReceiving = receiveItems.reduce((sum, item) => sum + item.receivedQuantity, 0)
  const allItemsHaveIssues = receiveItems.every(item => item.condition !== 'Good' || item.receivedQuantity < item.remainingQuantity)

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-xlarge" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Receive Items - {purchaseOrder.poNumber}</h2>
          <button onClick={onClose} className="modal-close" aria-label="Close">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="form-error">{error}</div>
            )}

            <div className="receiving-header">
              <div className="form-group">
                <label htmlFor="receipt-date">Receipt Date *</label>
                <input
                  id="receipt-date"
                  type="date"
                  value={receiptDate}
                  onChange={e => setReceiptDate(e.target.value)}
                  required
                />
              </div>

              <div className="supplier-info">
                <span className="label">Supplier:</span>
                <span className="value">{purchaseOrder.supplierName}</span>
              </div>
            </div>

            {loading ? (
              <div className="loading-state">Loading order details...</div>
            ) : receiveItems.length === 0 ? (
              <div className="empty-state">
                <p>No items to receive.</p>
              </div>
            ) : (
              <>
                <div className="table-container">
                  <table className="receiving-table">
                    <thead>
                      <tr>
                        <th>Product</th>
                        <th>Ordered</th>
                        <th>Previously Received</th>
                        <th>Remaining</th>
                        <th>Receiving Now</th>
                        <th>Condition</th>
                        <th>Notes</th>
                      </tr>
                    </thead>
                    <tbody>
                      {receiveItems.map(item => (
                        <tr key={item.purchaseOrderItemId}>
                          <td>
                            <div className="product-info">
                              <span className="product-name">{item.productName}</span>
                              {item.sku && <span className="product-sku">{item.sku}</span>}
                            </div>
                          </td>
                          <td>{item.orderedQuantity}</td>
                          <td>
                            <span className={item.previouslyReceived > 0 ? 'text-success' : ''}>
                              {item.previouslyReceived}
                            </span>
                          </td>
                          <td>
                            <span className={item.remainingQuantity > 0 ? 'text-warning' : 'text-success'}>
                              {item.remainingQuantity}
                            </span>
                          </td>
                          <td>
                            <input
                              type="number"
                              min="0"
                              max={item.remainingQuantity}
                              step="0.01"
                              value={item.receivedQuantity}
                              onChange={e => handleItemChange(
                                item.purchaseOrderItemId,
                                'receivedQuantity',
                                parseFloat(e.target.value) || 0
                              )}
                              className="quantity-input"
                              disabled={item.remainingQuantity === 0}
                            />
                          </td>
                          <td>
                            <select
                              value={item.condition}
                              onChange={e => handleItemChange(
                                item.purchaseOrderItemId,
                                'condition',
                                e.target.value as ReceiptItemCondition
                              )}
                              className="condition-select"
                              disabled={item.remainingQuantity === 0}
                            >
                              {CONDITION_OPTIONS.map(opt => (
                                <option key={opt.value} value={opt.value}>{opt.label}</option>
                              ))}
                            </select>
                          </td>
                          <td>
                            <input
                              type="text"
                              value={item.notes}
                              onChange={e => handleItemChange(
                                item.purchaseOrderItemId,
                                'notes',
                                e.target.value
                              )}
                              placeholder="Optional notes..."
                              className="notes-input"
                              disabled={item.remainingQuantity === 0}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="receiving-summary">
                  <div className="summary-item">
                    <span className="summary-label">Total items receiving:</span>
                    <span className="summary-value">{totalReceiving}</span>
                  </div>
                  {allItemsHaveIssues && totalReceiving > 0 && (
                    <div className="summary-warning">
                      Some items have issues or are only partially received. The PO will remain open.
                    </div>
                  )}
                </div>
              </>
            )}

            <div className="form-group" style={{ marginTop: '1rem' }}>
              <label htmlFor="receipt-notes">Receipt Notes</label>
              <textarea
                id="receipt-notes"
                value={receiptNotes}
                onChange={e => setReceiptNotes(e.target.value)}
                rows={3}
                placeholder="General notes about this receipt (e.g., delivery conditions, carrier info)..."
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="button-secondary" disabled={saving}>
              Cancel
            </button>
            <button
              type="submit"
              className="button-success"
              disabled={saving || loading || totalReceiving === 0}
            >
              {saving ? 'Saving...' : 'Confirm Receipt'}
            </button>
          </div>
        </form>

        <style>{`
          .modal-xlarge {
            max-width: 1000px;
          }

          .form-error {
            background: var(--color-error-bg, rgba(239, 68, 68, 0.1));
            color: var(--color-error);
            padding: 0.75rem;
            border-radius: 4px;
            margin-bottom: 1rem;
          }

          .receiving-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-end;
            margin-bottom: 1.5rem;
            padding-bottom: 1rem;
            border-bottom: 1px solid var(--color-border);
          }

          .form-group {
            margin-bottom: 1rem;
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

          .supplier-info {
            text-align: right;
          }

          .supplier-info .label {
            color: var(--color-text-secondary);
          }

          .supplier-info .value {
            font-weight: 500;
            margin-left: 0.5rem;
          }

          .table-container {
            overflow-x: auto;
          }

          .receiving-table {
            width: 100%;
            border-collapse: collapse;
          }

          .receiving-table th,
          .receiving-table td {
            padding: 0.5rem;
            text-align: left;
            border-bottom: 1px solid var(--color-border);
          }

          .receiving-table th {
            font-size: 0.75rem;
            text-transform: uppercase;
            color: var(--color-text-secondary);
            background: var(--color-surface);
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

          .text-success {
            color: var(--color-success, #10B981);
          }

          .text-warning {
            color: var(--color-warning, #F59E0B);
          }

          .quantity-input {
            width: 80px;
            padding: 0.25rem 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
          }

          .quantity-input:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }

          .condition-select {
            padding: 0.25rem 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
            min-width: 100px;
          }

          .condition-select:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }

          .notes-input {
            width: 150px;
            padding: 0.25rem 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
          }

          .notes-input:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }

          .receiving-summary {
            margin-top: 1rem;
            padding: 1rem;
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 8px;
          }

          .summary-item {
            display: flex;
            justify-content: space-between;
          }

          .summary-label {
            color: var(--color-text-secondary);
          }

          .summary-value {
            font-weight: 600;
            font-size: 1.25rem;
          }

          .summary-warning {
            margin-top: 0.5rem;
            padding: 0.5rem;
            background: rgba(245, 158, 11, 0.1);
            color: var(--color-warning, #F59E0B);
            border-radius: 4px;
            font-size: 0.875rem;
          }

          .loading-state,
          .empty-state {
            text-align: center;
            padding: 2rem;
            color: var(--color-text-secondary);
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

          .button-secondary {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            color: var(--color-text);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }

          .button-secondary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }

          .button-success {
            background: var(--color-success, #10B981);
            color: white;
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }

          .button-success:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }
        `}</style>
      </div>
    </div>
  )
}

export default ReceivingModal
