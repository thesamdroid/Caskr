import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchOrders,
  addOrder,
  updateOrder,
  deleteOrder,
  Order
} from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'

function OrdersPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)

  const [newName, setNewName] = useState('')
  const [newStatus, setNewStatus] = useState<number>(0)
  const [editing, setEditing] = useState<number | null>(null)
  const [editName, setEditName] = useState('')
  const [editStatus, setEditStatus] = useState<number>(0)

  useEffect(() => {
    dispatch(fetchOrders())
    dispatch(fetchStatuses())
  }, [dispatch])

  useEffect(() => {
    if (statuses.length > 0 && newStatus === 0) {
      setNewStatus(statuses[0].id)
    }
  }, [statuses, newStatus])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addOrder({ name: newName, statusId: newStatus, ownerId: 1, spiritTypeId: 1, quantity: 1, mashBillId: 1 }))
    setNewName('')
  }

  const startEdit = (order: Order) => {
    setEditing(order.id)
    setEditName(order.name)
    setEditStatus(order.statusId)
  }

  const handleUpdate = (id: number) => {
    const existing = orders.find(o => o.id === id)
    if (!existing) return
    dispatch(
      updateOrder({
        ...existing,
        name: editName,
        statusId: editStatus
      })
    )
    setEditing(null)
  }

  const getStatusName = (id: number) => {
    return statuses.find(s => s.id === id)?.name || id
  }

  return (
    <div>
      <h1>Orders</h1>
      <form onSubmit={handleAdd}>
        <input value={newName} onChange={e => setNewName(e.target.value)} placeholder='Name' />
        <select value={newStatus} onChange={e => setNewStatus(Number(e.target.value))}>
          {statuses.map(s => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <button type='submit'>Add</button>
      </form>
      <table className='table'>
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(o => (
            <tr key={o.id}>
              <td>
                {editing === o.id ? (
                  <input value={editName} onChange={e => setEditName(e.target.value)} />
                ) : (
                  o.name
                )}
              </td>
              <td>
                {editing === o.id ? (
                  <select value={editStatus} onChange={e => setEditStatus(Number(e.target.value))}>
                    {statuses.map(s => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                ) : (
                  getStatusName(o.statusId)
                )}
              </td>
              <td>
                {editing === o.id ? (
                  <>
                    <button onClick={() => handleUpdate(o.id)}>Save</button>
                    <button onClick={() => setEditing(null)}>Cancel</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => startEdit(o)}>Edit</button>
                    <button onClick={() => dispatch(deleteOrder(o.id))}>Delete</button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default OrdersPage
