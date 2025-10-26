import { useEffect, useMemo, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchOrders,
  addOrder,
  updateOrder,
  deleteOrder,
  fetchOutstandingTasks,
  Order
} from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import OrderActionsModal from '../components/OrderActionsModal'

function OrdersPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const outstandingTasks = useAppSelector(state => state.orders.outstandingTasks)

  const [newName, setNewName] = useState('')
  const [newStatus, setNewStatus] = useState<number>(0)
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null)

  useEffect(() => {
    dispatch(fetchOrders())
    dispatch(fetchStatuses())
  }, [dispatch])

  useEffect(() => {
    if (statuses.length > 0 && newStatus === 0) {
      setNewStatus(statuses[0].id)
    }
  }, [statuses, newStatus])

  const statusNames = useMemo(
    () => Object.fromEntries(statuses.map(status => [status.id, status.name])),
    [statuses]
  )

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addOrder({ name: newName, statusId: newStatus, ownerId: 1, spiritTypeId: 1, quantity: 1, mashBillId: 1 }))
    setNewName('')
  }

  const handleOpenActions = (order: Order) => {
    setSelectedOrder(order)
    dispatch(fetchOutstandingTasks(order.id))
  }

  const handleCloseActions = () => {
    setSelectedOrder(null)
  }

  const handleUpdate = (order: Order) => {
    dispatch(updateOrder(order))
      .unwrap()
      .then(updated => {
        setSelectedOrder(updated)
      })
      .catch(error => {
        console.error('[OrdersPage] Failed to update order', { orderId: order.id, error })
      })
  }

  const handleDelete = (id: number) => {
    dispatch(deleteOrder(id))
  }

  return (
    <section className='content-section'>
      <div className='section-header'>
        <h2 className='section-title'>Orders</h2>
      </div>
      <form onSubmit={handleAdd} className='inline-form'>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name' />
        <select value={newStatus} onChange={e => setNewStatus(Number(e.target.value))}>
          {statuses.map(s => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <button type='submit'>Add</button>
      </form>
      <div className='table-container'>
        <table className='table'>
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Outstanding Tasks</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(order => (
              <tr key={order.id} onClick={() => handleOpenActions(order)} className='clickable-row'>
                <td>{order.name}</td>
                <td>{statusNames[order.statusId] ?? order.statusId}</td>
                <td>{outstandingTasks[order.id]?.length ?? '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <OrderActionsModal
        isOpen={selectedOrder !== null}
        order={selectedOrder}
        statuses={statuses}
        tasks={selectedOrder ? outstandingTasks[selectedOrder.id] : undefined}
        onClose={handleCloseActions}
        onUpdate={handleUpdate}
        onDelete={handleDelete}
      />
    </section>
  )
}

export default OrdersPage
