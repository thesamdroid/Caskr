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
    <section className="content-section" aria-labelledby="orders-title">
      <div className="section-header">
        <div>
          <h1 id="orders-title" className="section-title">Orders</h1>
          <p className="section-subtitle">Manage and track all your orders</p>
        </div>
      </div>

      <form onSubmit={handleAdd} className="inline-form" aria-label="Add new order">
        <label htmlFor="order-name" className="visually-hidden">Order Name</label>
        <input
          id="order-name"
          value={newName}
          onChange={e => setNewName(e.target.value)}
          placeholder="Order name"
          required
          aria-required="true"
        />

        <label htmlFor="order-status" className="visually-hidden">Order Status</label>
        <select
          id="order-status"
          value={newStatus}
          onChange={e => setNewStatus(Number(e.target.value))}
          required
          aria-required="true"
        >
          {statuses.map(s => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>

        <button type="submit" className="button-primary">
          Add Order
        </button>
      </form>

      {orders.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">📦</div>
          <h3 className="empty-state-title">No orders yet</h3>
          <p className="empty-state-text">Create your first order using the form above</p>
        </div>
      ) : (
        <div className="table-container">
          <table className="table" role="table" aria-label="Orders list">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Status</th>
                <th scope="col">Outstanding Tasks</th>
              </tr>
            </thead>
            <tbody>
              {orders.map(order => {
                const statusName = statusNames[order.statusId]
                const statusClass = statusName
                  ? statusName.toLowerCase().replace(/\s+/g, '-')
                  : 'default'

                return (
                  <tr
                    key={order.id}
                    onClick={() => handleOpenActions(order)}
                    className="clickable-row"
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault()
                        handleOpenActions(order)
                      }
                    }}
                    aria-label={`View details for order ${order.name}`}
                  >
                    <td>{order.name}</td>
                    <td>
                      <span className={`status-badge ${statusClass}`}>
                        {statusName ?? order.statusId}
                      </span>
                    </td>
                    <td>
                      {outstandingTasks[order.id]?.length > 0 ? (
                        <span className="text-gold">{outstandingTasks[order.id].length}</span>
                      ) : (
                        <span className="text-muted">-</span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

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
