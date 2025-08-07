import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchOrders, addOrder, Order, fetchOutstandingTasks } from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import './LandingPage.css'

function LandingPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const outstandingTasks = useAppSelector(state => state.orders.outstandingTasks)

  const [showForm, setShowForm] = useState(false)
  const [newName, setNewName] = useState('')
  const [newStatus, setNewStatus] = useState<number>(0)

  useEffect(() => {
    dispatch(fetchOrders())
    dispatch(fetchStatuses())
  }, [dispatch])

  useEffect(() => {
    orders.forEach(o => {
      if (!outstandingTasks[o.id]) {
        dispatch(fetchOutstandingTasks(o.id))
      }
    })
  }, [orders, outstandingTasks, dispatch])

  useEffect(() => {
    if (statuses.length > 0 && newStatus === 0) {
      setNewStatus(statuses[0].id)
    }
  }, [statuses, newStatus])

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(addOrder({ name: newName, statusId: newStatus }))
    setNewName('')
    setShowForm(false)
  }

  const getStatusName = (id: number) => {
    return statuses.find(s => s.id === id)?.name || id
  }

  return (
    <div className='landing'>
      <h1>Current Orders</h1>
      <table className='orders-table'>
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Outstanding Tasks</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((o: Order) => (
            <tr key={o.id}>
              <td>{o.name}</td>
              <td>{getStatusName(o.statusId)}</td>
              <td>{(outstandingTasks[o.id] ?? []).map(t => t.name).join(', ')}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {showForm ? (
        <form className='order-form' onSubmit={handleAdd}>
          <input
            value={newName}
            onChange={e => setNewName(e.target.value)}
            placeholder='Order name'
          />
          <select value={newStatus} onChange={e => setNewStatus(Number(e.target.value))}>
            {statuses.map(s => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          <button type='submit'>Save</button>
        </form>
      ) : (
        <button className='create-button' onClick={() => setShowForm(true)}>
          Create New Order
        </button>
      )}
    </div>
  )
}

export default LandingPage
