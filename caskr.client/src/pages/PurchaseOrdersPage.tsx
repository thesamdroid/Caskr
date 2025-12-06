import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchPurchaseOrders,
  PurchaseOrder,
  PurchaseOrderStatus,
  cancelPurchaseOrder,
  deletePurchaseOrder
} from '../features/purchaseOrdersSlice'
import { fetchSuppliers } from '../features/suppliersSlice'
import PurchaseOrderFormModal from '../components/PurchaseOrderFormModal'
import PurchaseOrderDetailModal from '../components/PurchaseOrderDetailModal'
import ReceivingModal from '../components/ReceivingModal'
import SendPOEmailModal from '../components/SendPOEmailModal'

const STATUS_OPTIONS: { value: PurchaseOrderStatus | ''; label: string }[] = [
  { value: '', label: 'All Statuses' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Sent', label: 'Sent' },
  { value: 'Confirmed', label: 'Confirmed' },
  { value: 'Partial_Received', label: 'Partial Received' },
  { value: 'Received', label: 'Received' },
  { value: 'Cancelled', label: 'Cancelled' }
]

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
    month: 'short',
    day: 'numeric'
  })
}

function PurchaseOrdersPage() {
  const dispatch = useAppDispatch()
  const purchaseOrders = useAppSelector(state => state.purchaseOrders.items)
  const loading = useAppSelector(state => state.purchaseOrders.loading)
  const suppliers = useAppSelector(state => state.suppliers.items)

  // Filter state
  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | ''>('')
  const [supplierFilter, setSupplierFilter] = useState<number | ''>('')
  const [startDateFilter, setStartDateFilter] = useState('')
  const [endDateFilter, setEndDateFilter] = useState('')

  // Modal state
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [editingPO, setEditingPO] = useState<PurchaseOrder | null>(null)
  const [viewingPO, setViewingPO] = useState<PurchaseOrder | null>(null)
  const [receivingPO, setReceivingPO] = useState<PurchaseOrder | null>(null)
  const [emailingPO, setEmailingPO] = useState<PurchaseOrder | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<PurchaseOrder | null>(null)
  const [confirmCancel, setConfirmCancel] = useState<PurchaseOrder | null>(null)

  useEffect(() => {
    dispatch(fetchSuppliers({ includeInactive: false }))
  }, [dispatch])

  useEffect(() => {
    dispatch(fetchPurchaseOrders({
      status: statusFilter || undefined,
      supplierId: supplierFilter || undefined,
      startDate: startDateFilter || undefined,
      endDate: endDateFilter || undefined
    }))
  }, [dispatch, statusFilter, supplierFilter, startDateFilter, endDateFilter])

  const handleCreatePO = () => {
    setEditingPO(null)
    setShowCreateModal(true)
  }

  const handleEditPO = (po: PurchaseOrder) => {
    setEditingPO(po)
    setShowCreateModal(true)
  }

  const handleViewPO = (po: PurchaseOrder) => {
    setViewingPO(po)
  }

  const handleReceive = (po: PurchaseOrder) => {
    setReceivingPO(po)
  }

  const handleSendEmail = (po: PurchaseOrder) => {
    setEmailingPO(po)
  }

  const handleConfirmDelete = async () => {
    if (confirmDelete) {
      try {
        await dispatch(deletePurchaseOrder(confirmDelete.id)).unwrap()
        setConfirmDelete(null)
      } catch (error: unknown) {
        const errorMessage = error && typeof error === 'object' && 'message' in error
          ? (error as { message: string }).message
          : 'Failed to delete purchase order'
        alert(errorMessage)
      }
    }
  }

  const handleConfirmCancel = async () => {
    if (confirmCancel) {
      try {
        await dispatch(cancelPurchaseOrder(confirmCancel.id)).unwrap()
        setConfirmCancel(null)
      } catch (error: unknown) {
        const errorMessage = error && typeof error === 'object' && 'message' in error
          ? (error as { message: string }).message
          : 'Failed to cancel purchase order'
        alert(errorMessage)
      }
    }
  }

  const handleCloseCreateModal = () => {
    setShowCreateModal(false)
    setEditingPO(null)
  }

  const handleCloseViewModal = () => {
    setViewingPO(null)
  }

  const handleCloseReceivingModal = () => {
    setReceivingPO(null)
    // Refresh the PO list after receiving
    dispatch(fetchPurchaseOrders({
      status: statusFilter || undefined,
      supplierId: supplierFilter || undefined,
      startDate: startDateFilter || undefined,
      endDate: endDateFilter || undefined
    }))
  }

  const handleCloseEmailModal = () => {
    setEmailingPO(null)
  }

  const clearFilters = () => {
    setStatusFilter('')
    setSupplierFilter('')
    setStartDateFilter('')
    setEndDateFilter('')
  }

  // Calculate summary stats
  const openPOs = purchaseOrders.filter(po => !['Received', 'Cancelled'].includes(po.status))
  const totalOpenValue = openPOs.reduce((sum, po) => sum + (po.totalAmount || 0), 0)
  const pendingDeliveries = purchaseOrders.filter(po => ['Sent', 'Confirmed', 'Partial_Received'].includes(po.status))

  return (
    <>
      <section className="content-section" aria-labelledby="po-title">
        <div className="section-header">
          <div>
            <h1 id="po-title" className="section-title">Purchase Orders</h1>
            <p className="section-subtitle">Manage supplier orders and track deliveries</p>
          </div>
          <div className="section-actions">
            <button
              onClick={handleCreatePO}
              className="button-primary"
              aria-label="Create new purchase order"
            >
              + Create PO
            </button>
          </div>
        </div>

        {/* Summary Cards */}
        <div className="stats-grid" style={{ marginBottom: '1.5rem' }}>
          <div className="stat-card">
            <span className="stat-label">Total POs</span>
            <span className="stat-value">{purchaseOrders.length}</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Open Orders</span>
            <span className="stat-value text-gold">{openPOs.length}</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Pending Deliveries</span>
            <span className="stat-value">{pendingDeliveries.length}</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Open Order Value</span>
            <span className="stat-value">{formatCurrency(totalOpenValue)}</span>
          </div>
        </div>

        {/* Filters */}
        <div className="filters-bar">
          <div className="filter-group">
            <label htmlFor="status-filter">Status</label>
            <select
              id="status-filter"
              value={statusFilter}
              onChange={e => setStatusFilter(e.target.value as PurchaseOrderStatus | '')}
            >
              {STATUS_OPTIONS.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="supplier-filter">Supplier</label>
            <select
              id="supplier-filter"
              value={supplierFilter}
              onChange={e => setSupplierFilter(e.target.value ? parseInt(e.target.value, 10) : '')}
            >
              <option value="">All Suppliers</option>
              {suppliers.map(s => (
                <option key={s.id} value={s.id}>{s.supplierName}</option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="start-date-filter">From Date</label>
            <input
              id="start-date-filter"
              type="date"
              value={startDateFilter}
              onChange={e => setStartDateFilter(e.target.value)}
            />
          </div>

          <div className="filter-group">
            <label htmlFor="end-date-filter">To Date</label>
            <input
              id="end-date-filter"
              type="date"
              value={endDateFilter}
              onChange={e => setEndDateFilter(e.target.value)}
            />
          </div>

          {(statusFilter || supplierFilter || startDateFilter || endDateFilter) && (
            <button
              onClick={clearFilters}
              className="button-small button-secondary"
              style={{ alignSelf: 'flex-end' }}
            >
              Clear Filters
            </button>
          )}
        </div>

        {loading ? (
          <div className="loading-state">
            <p>Loading purchase orders...</p>
          </div>
        ) : purchaseOrders.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📦</div>
            <h3 className="empty-state-title">No purchase orders found</h3>
            <p className="empty-state-text">
              {statusFilter || supplierFilter || startDateFilter || endDateFilter
                ? 'Try adjusting your filters to see more results'
                : 'Create your first purchase order to start managing supplier orders'}
            </p>
            <button onClick={handleCreatePO} className="button-primary">
              + Create PO
            </button>
          </div>
        ) : (
          <div className="table-container">
            <table className="table" role="table" aria-label="Purchase Orders">
              <thead>
                <tr>
                  <th scope="col">PO Number</th>
                  <th scope="col">Supplier</th>
                  <th scope="col">Order Date</th>
                  <th scope="col">Delivery Date</th>
                  <th scope="col">Status</th>
                  <th scope="col">Total</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {purchaseOrders.map(po => (
                  <tr
                    key={po.id}
                    className={po.status === 'Cancelled' ? 'row-inactive' : ''}
                  >
                    <td>
                      <button
                        onClick={() => handleViewPO(po)}
                        className="link-button text-gold"
                        title="View PO details"
                      >
                        {po.poNumber}
                      </button>
                    </td>
                    <td>
                      <span>{po.supplierName}</span>
                    </td>
                    <td>
                      <span className="text-secondary">{formatDate(po.orderDate)}</span>
                    </td>
                    <td>
                      <span className="text-secondary">
                        {po.expectedDeliveryDate ? formatDate(po.expectedDeliveryDate) : '-'}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${getStatusBadgeClass(po.status)}`}>
                        {formatStatus(po.status)}
                      </span>
                    </td>
                    <td>
                      <span>{formatCurrency(po.totalAmount || 0, po.currency)}</span>
                    </td>
                    <td>
                      <div className="action-buttons">
                        <button
                          onClick={() => handleViewPO(po)}
                          className="button-small button-secondary"
                          title="View details"
                        >
                          View
                        </button>

                        {po.status === 'Draft' && (
                          <>
                            <button
                              onClick={() => handleEditPO(po)}
                              className="button-small button-secondary"
                              title="Edit PO"
                            >
                              Edit
                            </button>
                            <button
                              onClick={() => handleSendEmail(po)}
                              className="button-small button-primary"
                              title="Send to supplier"
                            >
                              Send
                            </button>
                            <button
                              onClick={() => setConfirmDelete(po)}
                              className="button-small button-danger"
                              title="Delete PO"
                            >
                              Delete
                            </button>
                          </>
                        )}

                        {['Sent', 'Confirmed', 'Partial_Received'].includes(po.status) && (
                          <>
                            <button
                              onClick={() => handleReceive(po)}
                              className="button-small button-success"
                              title="Mark as received"
                            >
                              Receive
                            </button>
                            <button
                              onClick={() => setConfirmCancel(po)}
                              className="button-small button-danger"
                              title="Cancel PO"
                            >
                              Cancel
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Create/Edit Modal */}
      <PurchaseOrderFormModal
        isOpen={showCreateModal}
        onClose={handleCloseCreateModal}
        purchaseOrder={editingPO}
      />

      {/* Detail Modal */}
      {viewingPO && (
        <PurchaseOrderDetailModal
          isOpen={!!viewingPO}
          onClose={handleCloseViewModal}
          purchaseOrder={viewingPO}
          onEdit={() => {
            handleCloseViewModal()
            handleEditPO(viewingPO)
          }}
          onSend={() => {
            handleCloseViewModal()
            handleSendEmail(viewingPO)
          }}
          onReceive={() => {
            handleCloseViewModal()
            handleReceive(viewingPO)
          }}
          onCancel={() => {
            handleCloseViewModal()
            setConfirmCancel(viewingPO)
          }}
          onDelete={() => {
            handleCloseViewModal()
            setConfirmDelete(viewingPO)
          }}
        />
      )}

      {/* Receiving Modal */}
      {receivingPO && (
        <ReceivingModal
          isOpen={!!receivingPO}
          onClose={handleCloseReceivingModal}
          purchaseOrder={receivingPO}
        />
      )}

      {/* Email Modal */}
      {emailingPO && (
        <SendPOEmailModal
          isOpen={!!emailingPO}
          onClose={handleCloseEmailModal}
          purchaseOrder={emailingPO}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {confirmDelete && (
        <div className="modal-overlay" onClick={() => setConfirmDelete(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Confirm Deletion</h2>
              <button
                onClick={() => setConfirmDelete(null)}
                className="modal-close"
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="modal-body">
              <p>
                Are you sure you want to delete PO <strong>{confirmDelete.poNumber}</strong>?
              </p>
              <p className="text-secondary">
                This action cannot be undone.
              </p>
            </div>
            <div className="modal-footer">
              <button
                onClick={() => setConfirmDelete(null)}
                className="button-secondary"
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmDelete}
                className="button-danger"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Cancel Confirmation Dialog */}
      {confirmCancel && (
        <div className="modal-overlay" onClick={() => setConfirmCancel(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Confirm Cancellation</h2>
              <button
                onClick={() => setConfirmCancel(null)}
                className="modal-close"
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="modal-body">
              <p>
                Are you sure you want to cancel PO <strong>{confirmCancel.poNumber}</strong>?
              </p>
              <p className="text-secondary">
                This will mark the order as cancelled and notify relevant parties.
              </p>
            </div>
            <div className="modal-footer">
              <button
                onClick={() => setConfirmCancel(null)}
                className="button-secondary"
              >
                Keep Order
              </button>
              <button
                onClick={handleConfirmCancel}
                className="button-danger"
              >
                Cancel PO
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
        .stats-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
          gap: 1rem;
        }

        .stat-card {
          background: var(--color-surface);
          border: 1px solid var(--color-border);
          border-radius: 8px;
          padding: 1rem;
          display: flex;
          flex-direction: column;
          align-items: center;
        }

        .stat-label {
          font-size: 0.875rem;
          color: var(--color-text-secondary);
          margin-bottom: 0.25rem;
        }

        .stat-value {
          font-size: 1.5rem;
          font-weight: 600;
          color: var(--color-text);
        }

        .text-gold {
          color: var(--gold-primary, #D4AF37);
        }

        .filters-bar {
          display: flex;
          flex-wrap: wrap;
          gap: 1rem;
          margin-bottom: 1.5rem;
          padding: 1rem;
          background: var(--color-surface);
          border: 1px solid var(--color-border);
          border-radius: 8px;
        }

        .filter-group {
          display: flex;
          flex-direction: column;
          gap: 0.25rem;
        }

        .filter-group label {
          font-size: 0.75rem;
          color: var(--color-text-secondary);
          text-transform: uppercase;
        }

        .filter-group select,
        .filter-group input {
          padding: 0.5rem;
          border: 1px solid var(--color-border);
          border-radius: 4px;
          background: var(--color-background);
          color: var(--color-text);
          min-width: 150px;
        }

        .filter-group select:focus,
        .filter-group input:focus {
          outline: none;
          border-color: var(--gold-primary, #D4AF37);
        }

        .row-inactive {
          opacity: 0.6;
        }

        .action-buttons {
          display: flex;
          flex-wrap: wrap;
          gap: 0.5rem;
        }

        .button-small {
          padding: 0.25rem 0.5rem;
          font-size: 0.75rem;
          border-radius: 4px;
          border: none;
          cursor: pointer;
          white-space: nowrap;
        }

        .button-secondary {
          background: var(--color-surface);
          border: 1px solid var(--color-border);
          color: var(--color-text);
        }

        .button-secondary:hover {
          background: var(--color-border);
        }

        .button-primary {
          background: var(--gold-primary, #D4AF37);
          color: var(--color-background);
        }

        .button-primary:hover {
          background: var(--gold-light, #E5C158);
        }

        .button-danger {
          background: var(--color-error, #EF4444);
          color: white;
        }

        .button-danger:hover {
          opacity: 0.9;
        }

        .button-success {
          background: var(--color-success, #10B981);
          color: white;
        }

        .button-success:hover {
          opacity: 0.9;
        }

        .link-button {
          background: none;
          border: none;
          padding: 0;
          cursor: pointer;
          text-decoration: underline;
          font-size: inherit;
        }

        .badge {
          padding: 0.25rem 0.5rem;
          border-radius: 4px;
          font-size: 0.75rem;
          font-weight: 500;
          white-space: nowrap;
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
          max-width: 500px;
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

        .loading-state {
          text-align: center;
          padding: 2rem;
          color: var(--color-text-secondary);
        }

        .empty-state {
          text-align: center;
          padding: 3rem;
        }

        .empty-state-icon {
          font-size: 3rem;
          margin-bottom: 1rem;
        }

        .empty-state-title {
          font-size: 1.25rem;
          margin-bottom: 0.5rem;
        }

        .empty-state-text {
          color: var(--color-text-secondary);
          margin-bottom: 1rem;
        }
      `}</style>
    </>
  )
}

export default PurchaseOrdersPage
