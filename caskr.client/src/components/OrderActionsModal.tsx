import { useEffect, useMemo, useState } from 'react'
import type { Order, Task } from '../features/ordersSlice'
import type { Status } from '../features/statusSlice'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchQuickBooksStatus, fetchInvoiceSyncStatus, syncInvoice } from '../features/accountingSlice'
import LabelModal from './LabelModal'
import TransferModal from './TransferModal'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  order: Order | null
  statuses: Status[]
  tasks?: Task[]
  onClose: () => void
  onUpdate: (order: Order) => void
  onDelete: (orderId: number) => void
}

const OrderActionsModal = ({
  isOpen,
  order,
  statuses,
  tasks,
  onClose,
  onUpdate,
  onDelete
}: Props) => {
  const dispatch = useAppDispatch()
  const quickBooksStatus = useAppSelector(state => state.accounting.status)
  const quickBooksStatusCompanyId = useAppSelector(state => state.accounting.statusCompanyId)
  const quickBooksStatusLoading = useAppSelector(state => state.accounting.statusLoading)
  const invoiceStatuses = useAppSelector(state => state.accounting.invoiceStatuses)
  const invoiceStatusLoading = useAppSelector(state => state.accounting.invoiceStatusLoading)
  const invoiceSyncing = useAppSelector(state => state.accounting.invoiceSyncing)
  const invoiceSyncErrors = useAppSelector(state => state.accounting.invoiceSyncErrors)

  const [isEditing, setIsEditing] = useState(false)
  const [editName, setEditName] = useState('')
  const [editStatusId, setEditStatusId] = useState<number | null>(null)
  const [isLabelModalOpen, setIsLabelModalOpen] = useState(false)
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false)

  useEffect(() => {
    if (isOpen && order) {
      setEditName(order.name)
      setEditStatusId(order.statusId)
      setIsEditing(false)
      setIsLabelModalOpen(false)
      setIsTransferModalOpen(false)
    }
  }, [isOpen, order])

  const statusName = useMemo(() => {
    if (!order) return ''
    return statuses.find(status => status.id === order.statusId)?.name ?? 'Unknown'
  }, [order, statuses])

  const isTtbStatus = statusName.toLowerCase().includes('ttb')

  const companyId = order?.companyId
  const invoiceId = order?.invoiceId ?? null
  const invoiceStatus = invoiceId ? invoiceStatuses[invoiceId] : undefined
  const isInvoiceStatusLoading = invoiceId ? invoiceStatusLoading[invoiceId] : false
  const isInvoiceSyncing = invoiceId ? invoiceSyncing[invoiceId] : false
  const invoiceSyncError = invoiceId ? invoiceSyncErrors[invoiceId] : null
  const statusMatchesCompany = companyId != null && quickBooksStatusCompanyId === companyId
  const isQuickBooksConnected = statusMatchesCompany && (quickBooksStatus?.connected ?? false)

  useEffect(() => {
    if (!isOpen || companyId == null) return
    if (!quickBooksStatus || quickBooksStatusCompanyId !== companyId) {
      dispatch(fetchQuickBooksStatus(companyId))
    }
  }, [dispatch, isOpen, companyId, quickBooksStatus, quickBooksStatusCompanyId])

  useEffect(() => {
    if (!isOpen || invoiceId == null) return
    dispatch(fetchInvoiceSyncStatus(invoiceId))
  }, [dispatch, isOpen, invoiceId])

  const handleSyncInvoice = async () => {
    if (!invoiceId) return
    try {
      await dispatch(syncInvoice(invoiceId)).unwrap()
      await dispatch(fetchInvoiceSyncStatus(invoiceId))
    } catch (error) {
      console.error('[OrderActionsModal] QuickBooks sync failed', error)
    }
  }

  if (!isOpen || !order) {
    return null
  }

  const handleSave = (event: React.FormEvent) => {
    event.preventDefault()
    if (editStatusId === null) return
    onUpdate({ ...order, name: editName, statusId: editStatusId })
    setIsEditing(false)
  }

  const handleDelete = () => {
    if (window.confirm(`Delete order "${order.name}"?`)) {
      onDelete(order.id)
      onClose()
    }
  }

  return (
    <>
      <div className='modal-overlay'>
        <div className='modal order-actions-modal'>
          <h2>{order.name}</h2>
          <p className='modal-subtitle'>Status: {statusName}</p>

          {tasks && (
            <div className='tasks-section'>
              <h3>Outstanding Tasks</h3>
              {tasks.length === 0 ? (
                <p className='tasks-empty'>All tasks complete</p>
              ) : (
                <ul className='tasks-list'>
                  {tasks.map(task => (
                    <li key={task.id}>{task.name}</li>
                  ))}
                </ul>
              )}
            </div>
          )}

          {isEditing ? (
            <form className='modal-form' onSubmit={handleSave}>
              <label>
                Name
                <input value={editName} onChange={event => setEditName(event.target.value)} />
              </label>
              <label>
                Status
                <select value={editStatusId ?? order.statusId} onChange={event => setEditStatusId(Number(event.target.value))}>
                  {statuses.map(status => (
                    <option key={status.id} value={status.id}>
                      {status.name}
                    </option>
                  ))}
                </select>
              </label>
              <div className='modal-actions'>
                <button type='submit'>Save</button>
                <button type='button' onClick={() => setIsEditing(false)}>Cancel</button>
              </div>
            </form>
          ) : (
            <div className='modal-actions'>
              <button type='button' onClick={() => setIsEditing(true)}>
                Edit Order
              </button>
              <button type='button' className='danger' onClick={handleDelete}>
                Delete Order
              </button>
              {isTtbStatus && (
                <button type='button' onClick={() => setIsLabelModalOpen(true)}>
                  Generate TTB Label
                </button>
              )}
              <button type='button' onClick={() => setIsTransferModalOpen(true)}>
                Generate Transfer Document
              </button>
              <button type='button' onClick={onClose}>
                Close
              </button>
            </div>
          )}

          <div className='quickbooks-sync-section'>
            <div className='quickbooks-sync-header'>
              <h3>QuickBooks Sync</h3>
              <span className={`sync-status-badge ${getSyncBadgeClass(invoiceStatus?.status, isInvoiceStatusLoading, isQuickBooksConnected)}`}>
                {getSyncStatusLabel(invoiceStatus?.status, isQuickBooksConnected, isInvoiceStatusLoading, !!invoiceId)}
              </span>
            </div>
            {!companyId ? (
              <p className='quickbooks-sync-details'>Company information unavailable for this order.</p>
            ) : quickBooksStatusLoading && !statusMatchesCompany ? (
              <p className='quickbooks-sync-details'>Loading QuickBooks connection…</p>
            ) : !isQuickBooksConnected ? (
              <p className='quickbooks-sync-details'>Connect QuickBooks to enable invoice sync.</p>
            ) : invoiceId == null ? (
              <p className='quickbooks-sync-details'>No invoice has been generated for this order yet.</p>
            ) : isInvoiceStatusLoading ? (
              <p className='quickbooks-sync-details'>Loading invoice sync status…</p>
            ) : (
              <>
                {invoiceStatus?.status === 'Success' && invoiceStatus.qboInvoiceId ? (
                  <p className='quickbooks-sync-details'>
                    QBO Invoice #:
                    <a href={`https://app.qbo.intuit.com/app/invoice?txnId=${invoiceStatus.qboInvoiceId}`} target='_blank' rel='noreferrer'>
                      {invoiceStatus.qboInvoiceId}
                    </a>
                  </p>
                ) : (
                  <p className='quickbooks-sync-details'>This invoice has not been synced to QuickBooks.</p>
                )}
                {invoiceStatus?.status === 'Failed' && invoiceStatus.errorMessage && (
                  <p className='quickbooks-sync-error'>Sync error: {invoiceStatus.errorMessage}</p>
                )}
                {invoiceSyncError && <p className='quickbooks-sync-error'>Action error: {invoiceSyncError}</p>}
                <div className='quickbooks-sync-actions'>
                  {(invoiceStatus?.status == null || invoiceStatus.status === 'Pending') && (
                    <button type='button' onClick={handleSyncInvoice} disabled={isInvoiceSyncing}>
                      {isInvoiceSyncing ? 'Syncing…' : 'Sync to QuickBooks'}
                    </button>
                  )}
                  {invoiceStatus?.status === 'Failed' && (
                    <button type='button' onClick={handleSyncInvoice} disabled={isInvoiceSyncing}>
                      {isInvoiceSyncing ? 'Retrying…' : 'Retry Sync'}
                    </button>
                  )}
                </div>
              </>
            )}
          </div>
        </div>
      </div>
      <LabelModal
        isOpen={isLabelModalOpen}
        onClose={() => setIsLabelModalOpen(false)}
        orderName={order.name}
        companyId={order.ownerId}
      />
      <TransferModal
        isOpen={isTransferModalOpen}
        onClose={() => setIsTransferModalOpen(false)}
        orderId={order.id}
        orderName={order.name}
      />
    </>
  )
}

const getSyncStatusLabel = (
  status: string | null | undefined,
  isConnected: boolean,
  isLoading: boolean,
  hasInvoice: boolean
) => {
  if (!hasInvoice) return 'No Invoice'
  if (!isConnected) return 'Disconnected'
  if (isLoading) return 'Checking…'
  if (!status) return 'Not Synced'
  switch (status) {
    case 'Pending':
      return 'Pending'
    case 'InProgress':
      return 'Syncing'
    case 'Success':
      return 'Synced'
    case 'Failed':
      return 'Sync Failed'
    default:
      return 'Not Synced'
  }
}

const getSyncBadgeClass = (
  status: string | null | undefined,
  isLoading: boolean,
  isConnected: boolean
) => {
  if (!isConnected) return 'not-synced'
  if (isLoading) return 'syncing'
  if (!status || status === 'Pending') return 'not-synced'
  if (status === 'InProgress') return 'syncing'
  if (status === 'Success') return 'success'
  if (status === 'Failed') return 'failed'
  return 'not-synced'
}

export default OrderActionsModal
