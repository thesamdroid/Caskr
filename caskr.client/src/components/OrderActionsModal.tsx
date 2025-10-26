import { useEffect, useMemo, useState } from 'react'
import type { Order } from '../features/ordersSlice'
import type { Status, StatusTask } from '../features/statusSlice'
import LabelModal from './LabelModal'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  order: Order | null
  statuses: Status[]
  tasks?: StatusTask[]
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
  const [isEditing, setIsEditing] = useState(false)
  const [editName, setEditName] = useState('')
  const [editStatusId, setEditStatusId] = useState<number | null>(null)
  const [isLabelModalOpen, setIsLabelModalOpen] = useState(false)

  useEffect(() => {
    if (isOpen && order) {
      setEditName(order.name)
      setEditStatusId(order.statusId)
      setIsEditing(false)
      setIsLabelModalOpen(false)
    }
  }, [isOpen, order])

  const statusName = useMemo(() => {
    if (!order) return ''
    return statuses.find(status => status.id === order.statusId)?.name ?? 'Unknown'
  }, [order, statuses])

  const isTtbStatus = statusName.toLowerCase().includes('ttb')

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
                  Generate TTB Document
                </button>
              )}
              <button type='button' onClick={onClose}>
                Close
              </button>
            </div>
          )}
        </div>
      </div>
      <LabelModal
        isOpen={isLabelModalOpen}
        onClose={() => setIsLabelModalOpen(false)}
        orderName={order.name}
        companyId={order.ownerId}
      />
    </>
  )
}

export default OrderActionsModal
