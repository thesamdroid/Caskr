import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchOrders,
  fetchOutstandingTasks
} from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import { fetchBarrels } from '../features/barrelsSlice'

export default function DashboardPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const tasks = useAppSelector(state => state.orders.outstandingTasks)
  const barrels = useAppSelector(state => state.barrels.items)

  useEffect(() => {
    dispatch(fetchOrders()).then(action => {
      if (fetchOrders.fulfilled.match(action)) {
        action.payload.forEach(order => dispatch(fetchOutstandingTasks(order.id)))
      }
    })
    dispatch(fetchStatuses())
    dispatch(fetchBarrels(1))
  }, [dispatch])

  const getStatusName = (id: number) =>
    statuses.find(s => s.id === id)?.name || 'Unknown'

  const getStatusClass = (id: number) => {
    const name = getStatusName(id).toLowerCase()
    if (name.includes('progress')) return 'in-progress'
    if (name.includes('complete')) return 'completed'
    return 'pending'
  }

  return (
    <>
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Active Orders</h2>
        </div>
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
                <tr key={order.id}>
                  <td>{order.name}</td>
                  <td>
                    <span className={`status-badge ${getStatusClass(order.statusId)}`}>
                      {getStatusName(order.statusId)}
                    </span>
                  </td>
                  <td>{tasks[order.id]?.length ?? 0}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Barrel Inventory</h2>
        </div>
        <div className='table-container'>
          <table className='table'>
            <thead>
              <tr>
                <th>SKU</th>
                <th>Order</th>
                <th>Location</th>
              </tr>
            </thead>
            <tbody>
              {barrels.map(b => (
                <tr key={b.id}>
                  <td>{b.sku}</td>
                  <td>{b.orderId}</td>
                  <td>{b.rickhouseId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </>
  )
}
