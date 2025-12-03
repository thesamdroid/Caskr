import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchPurchaseOrder,
  fetchInventoryReceipts,
  generatePOPdf,
  clearPdfUrl,
  PurchaseOrder,
  PurchaseOrderStatus
} from '../features/purchaseOrdersSlice'

interface PurchaseOrderDetailModalProps {
  isOpen: boolean
  onClose: () => void
  purchaseOrder: PurchaseOrder
  onEdit: () => void
  onSend: () => void
  onReceive: () => void
  onCancel: () => void
  onDelete: () => void
}

function getStatusBadgeClass(status: PurchaseOrderStatus): string {
  switch (status) {
    case 'Draft':
      return 'badge-secondary'
    case 'Sent':
      return 'badge-info'
    case 'Confirmed':
      return 'badge-primary'
    case 'Partial_Received':
      return 'badge-warning'
    case 'Received':
      return 'badge-success'
    case 'Cancelled':
      return 'badge-inactive'
    default:
      return 'badge-secondary'
  }
}

function formatStatus(status: PurchaseOrderStatus): string {
  return status.replace('_', ' ')
}

function formatCurrency(amount: number, currency: string = 'USD'): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency
  }).format(amount)
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

function PurchaseOrderDetailModal({
  isOpen,
  onClose,
  purchaseOrder,
  onEdit,
  onSend,
  onReceive,
  onCancel,
  onDelete
}: PurchaseOrderDetailModalProps) {
  const dispatch = useAppDispatch()
  const currentPO = useAppSelector(state => state.purchaseOrders.currentPurchaseOrder)
  const receipts = useAppSelector(state => state.purchaseOrders.receipts)
  const loading = useAppSelector(state => state.purchaseOrders.loading)

  // Fetch full PO details and receipts
  useEffect(() => {
    if (isOpen && purchaseOrder) {
      dispatch(fetchPurchaseOrder(purchaseOrder.id))
      dispatch(fetchInventoryReceipts(purchaseOrder.id))
    }
  }, [dispatch, isOpen, purchaseOrder])

  // Cleanup PDF URL on unmount
  useEffect(() => {
    return () => {
      dispatch(clearPdfUrl())
    }
  }, [dispatch])

  const handleDownloadPdf = async () => {
    const resultAction = await dispatch(generatePOPdf(purchaseOrder.id))
    if (generatePOPdf.fulfilled.match(resultAction)) {
      const url = resultAction.payload
      const link = document.createElement('a')
      link.href = url
      link.download = `${purchaseOrder.poNumber}.pdf`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
    }
  }

  if (!isOpen) return null

  const po = currentPO || purchaseOrder
  const items = currentPO?.items || []

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-xlarge" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="po-header-info">
            <h2>{po.poNumber}</h2>
            <span className={`badge ${getStatusBadgeClass(po.status)}`}>
              {formatStatus(po.status)}
            </span>
          </div>
          <button onClick={onClose} className="modal-close" aria-label="Close">
            &times;
          </button>
        </div>

        <div className="modal-body">
          {loading ? (
            <div className="loading-state">Loading details...</div>
          ) : (
            <>
              {/* PO Header Details */}
              <div className="detail-grid">
                <div className="detail-card">
                  <h4>Supplier</h4>
                  <p className="detail-value">{po.supplierName}</p>
                  {po.supplierEmail && (
                    <p className="detail-secondary">{po.supplierEmail}</p>
                  )}
                </div>

                <div className="detail-card">
                  <h4>Order Date</h4>
                  <p className="detail-value">{formatDate(po.orderDate)}</p>
                </div>

                <div className="detail-card">
                  <h4>Expected Delivery</h4>
                  <p className="detail-value">
                    {po.expectedDeliveryDate ? formatDate(po.expectedDeliveryDate) : 'Not specified'}
                  </p>
                </div>

                <div className="detail-card">
                  <h4>Total Amount</h4>
                  <p className="detail-value text-gold">
                    {formatCurrency(po.totalAmount || 0, po.currency)}
                  </p>
                </div>

                <div className="detail-card">
                  <h4>Payment Status</h4>
                  <p className="detail-value">{po.paymentStatus}</p>
                </div>

                <div className="detail-card">
                  <h4>Created By</h4>
                  <p className="detail-value">{po.createdByUserName || '-'}</p>
                  <p className="detail-secondary">{formatDate(po.createdAt)}</p>
                </div>
              </div>

              {/* Line Items */}
              <section className="detail-section">
                <h3>Line Items</h3>
                {items.length === 0 ? (
                  <p className="text-secondary">No line items loaded.</p>
                ) : (
                  <div className="table-container">
                    <table className="detail-table">
                      <thead>
                        <tr>
                          <th>Product</th>
                          <th>Quantity</th>
                          <th>Received</th>
                          <th>Unit Price</th>
                          <th>Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {items.map(item => (
                          <tr key={item.id}>
                            <td>
                              <div className="product-info">
                                <span className="product-name">{item.productName}</span>
                                {item.sku && <span className="product-sku">{item.sku}</span>}
                              </div>
                            </td>
                            <td>
                              {item.quantity} {item.unitOfMeasure || 'each'}
                            </td>
                            <td>
                              <span className={item.receivedQuantity >= item.quantity ? 'text-success' : item.receivedQuantity > 0 ? 'text-warning' : ''}>
                                {item.receivedQuantity} / {item.quantity}
                              </span>
                            </td>
                            <td>{formatCurrency(item.unitPrice)}</td>
                            <td>{formatCurrency(item.totalPrice)}</td>
                          </tr>
                        ))}
                      </tbody>
                      <tfoot>
                        <tr>
                          <td colSpan={4} className="total-label">Grand Total</td>
                          <td className="total-value">{formatCurrency(po.totalAmount || 0)}</td>
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                )}
              </section>

              {/* Receiving History */}
              {receipts.length > 0 && (
                <section className="detail-section">
                  <h3>Receiving History</h3>
                  <div className="receipts-list">
                    {receipts.map(receipt => (
                      <div key={receipt.id} className="receipt-card">
                        <div className="receipt-header">
                          <span className="receipt-date">
                            Received on {formatDate(receipt.receiptDate)}
                          </span>
                          {receipt.receivedByUserName && (
                            <span className="receipt-user">by {receipt.receivedByUserName}</span>
                          )}
                        </div>
                        {receipt.items && receipt.items.length > 0 && (
                          <ul className="receipt-items">
                            {receipt.items.map(item => (
                              <li key={item.id}>
                                {item.productName}: {item.receivedQuantity} received
                                <span className={`condition-badge condition-${item.condition.toLowerCase()}`}>
                                  {item.condition}
                                </span>
                                {item.notes && <span className="item-notes">{item.notes}</span>}
                              </li>
                            ))}
                          </ul>
                        )}
                        {receipt.notes && (
                          <p className="receipt-notes">{receipt.notes}</p>
                        )}
                      </div>
                    ))}
                  </div>
                </section>
              )}

              {/* Notes */}
              {po.notes && (
                <section className="detail-section">
                  <h3>Notes</h3>
                  <p className="notes-content">{po.notes}</p>
                </section>
              )}
            </>
          )}
        </div>

        <div className="modal-footer">
          <div className="footer-left">
            <button
              onClick={handleDownloadPdf}
              className="button-secondary"
              disabled={loading}
            >
              Download PDF
            </button>
          </div>

          <div className="footer-right">
            {po.status === 'Draft' && (
              <>
                <button onClick={onEdit} className="button-secondary">
                  Edit
                </button>
                <button onClick={onSend} className="button-primary">
                  Send to Supplier
                </button>
                <button onClick={onDelete} className="button-danger">
                  Delete
                </button>
              </>
            )}

            {['Sent', 'Confirmed', 'Partial_Received'].includes(po.status) && (
              <>
                <button onClick={onReceive} className="button-success">
                  Mark as Received
                </button>
                <button onClick={onCancel} className="button-danger">
                  Cancel PO
                </button>
              </>
            )}

            <button onClick={onClose} className="button-secondary">
              Close
            </button>
          </div>
        </div>

        <style>{`
          .modal-xlarge {
            max-width: 900px;
          }

          .po-header-info {
            display: flex;
            align-items: center;
            gap: 1rem;
          }

          .po-header-info h2 {
            margin: 0;
          }

          .detail-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1rem;
            margin-bottom: 1.5rem;
          }

          .detail-card {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 8px;
            padding: 1rem;
          }

          .detail-card h4 {
            margin: 0 0 0.25rem 0;
            font-size: 0.75rem;
            text-transform: uppercase;
            color: var(--color-text-secondary);
          }

          .detail-value {
            margin: 0;
            font-size: 1.125rem;
            font-weight: 500;
          }

          .detail-secondary {
            margin: 0.25rem 0 0 0;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
          }

          .text-gold {
            color: var(--gold-primary, #D4AF37);
          }

          .text-success {
            color: var(--color-success, #10B981);
          }

          .text-warning {
            color: var(--color-warning, #F59E0B);
          }

          .detail-section {
            margin-bottom: 1.5rem;
          }

          .detail-section h3 {
            margin: 0 0 1rem 0;
            font-size: 1rem;
            border-bottom: 1px solid var(--color-border);
            padding-bottom: 0.5rem;
          }

          .table-container {
            overflow-x: auto;
          }

          .detail-table {
            width: 100%;
            border-collapse: collapse;
          }

          .detail-table th,
          .detail-table td {
            padding: 0.75rem;
            text-align: left;
            border-bottom: 1px solid var(--color-border);
          }

          .detail-table th {
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

          .total-label {
            text-align: right;
            font-weight: 600;
          }

          .total-value {
            font-weight: 600;
            font-size: 1.125rem;
            color: var(--gold-primary, #D4AF37);
          }

          .receipts-list {
            display: flex;
            flex-direction: column;
            gap: 1rem;
          }

          .receipt-card {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 8px;
            padding: 1rem;
          }

          .receipt-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 0.5rem;
          }

          .receipt-date {
            font-weight: 500;
          }

          .receipt-user {
            color: var(--color-text-secondary);
          }

          .receipt-items {
            margin: 0;
            padding-left: 1.5rem;
          }

          .receipt-items li {
            margin-bottom: 0.25rem;
          }

          .condition-badge {
            margin-left: 0.5rem;
            padding: 0.125rem 0.375rem;
            border-radius: 4px;
            font-size: 0.75rem;
          }

          .condition-good {
            background: rgba(16, 185, 129, 0.2);
            color: #10B981;
          }

          .condition-damaged {
            background: rgba(239, 68, 68, 0.2);
            color: #EF4444;
          }

          .condition-partial {
            background: rgba(245, 158, 11, 0.2);
            color: #F59E0B;
          }

          .item-notes {
            display: block;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
            margin-top: 0.25rem;
          }

          .receipt-notes {
            margin: 0.5rem 0 0 0;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
            font-style: italic;
          }

          .notes-content {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 8px;
            padding: 1rem;
            margin: 0;
            white-space: pre-wrap;
          }

          .modal-footer {
            display: flex;
            justify-content: space-between;
            padding: 1rem;
            border-top: 1px solid var(--color-border);
          }

          .footer-left,
          .footer-right {
            display: flex;
            gap: 0.5rem;
          }

          .badge {
            padding: 0.25rem 0.5rem;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: 500;
          }

          .badge-secondary {
            background: var(--color-surface);
            color: var(--color-text-secondary);
            border: 1px solid var(--color-border);
          }

          .badge-info {
            background: rgba(96, 165, 250, 0.2);
            color: #60A5FA;
          }

          .badge-primary {
            background: rgba(212, 175, 55, 0.2);
            color: var(--gold-primary, #D4AF37);
          }

          .badge-warning {
            background: rgba(245, 158, 11, 0.2);
            color: #F59E0B;
          }

          .badge-success {
            background: rgba(16, 185, 129, 0.2);
            color: #10B981;
          }

          .badge-inactive {
            background: var(--color-surface);
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

          .loading-state {
            text-align: center;
            padding: 2rem;
            color: var(--color-text-secondary);
          }

          .button-primary {
            background: var(--gold-primary, #D4AF37);
            color: var(--color-background);
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }

          .button-secondary {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            color: var(--color-text);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }

          .button-danger {
            background: var(--color-error, #EF4444);
            color: white;
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }

          .button-success {
            background: var(--color-success, #10B981);
            color: white;
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
          }
        `}</style>
      </div>
    </div>
  )
}

export default PurchaseOrderDetailModal
