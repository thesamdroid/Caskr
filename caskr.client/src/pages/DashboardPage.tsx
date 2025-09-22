import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchOrders,
  fetchOutstandingTasks,
  updateOrder,
  deleteOrder,
  type Order
} from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import { fetchBarrels, forecastBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'
import OrderActionsModal from '../components/OrderActionsModal'
import { formatForecastSummary } from '../utils/forecastSummary'

export default function DashboardPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const tasks = useAppSelector(state => state.orders.outstandingTasks)
  const barrels = useAppSelector(state => state.barrels.items)
  const [showForecastModal, setShowForecastModal] = useState(false)
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null)
  const [forecastError, setForecastError] = useState<string | null>(null)
  const [forecastResult, setForecastResult] = useState<
    { date: string; count: number; ageYears: number }
  | null>(null)

  useEffect(() => {
    dispatch(fetchOrders()).then(action => {
      if (fetchOrders.fulfilled.match(action)) {
        action.payload.forEach(order => dispatch(fetchOutstandingTasks(order.id)))
      }
    })
    dispatch(fetchStatuses())
    dispatch(fetchBarrels(1))
  }, [dispatch])

  const handleForecastSubmit = async (targetDate: string, ageYears: number) => {
    setForecastError(null)
    try {
      const result = await dispatch(forecastBarrels({ companyId: 1, targetDate, ageYears })).unwrap()
      setForecastResult({ date: targetDate, count: result.count, ageYears })
      setShowForecastModal(false)
    } catch (error) {
      setForecastError('Unable to forecast barrels right now. Please try again later.')
      throw error
    }
  }

  const getStatusName = (id: number) =>
    statuses.find(s => s.id === id)?.name || 'Unknown'

  const getStatusClass = (id: number) => {
    const name = getStatusName(id).toLowerCase()
    if (name.includes('progress')) return 'in-progress'
    if (name.includes('complete')) return 'completed'
    return 'pending'
  }

  const handleOrderClick = (order: Order) => {
    setSelectedOrder(order)
    dispatch(fetchOutstandingTasks(order.id))
  }

  const handleCloseOrderModal = () => {
    setSelectedOrder(null)
  }

  const handleUpdateOrder = (order: Order) => {
    dispatch(updateOrder(order))
      .unwrap()
      .then(updated => {
        setSelectedOrder(updated)
      })
      .catch(() => {
        // ignore failure; toast/notification system not available yet
      })
  }

  const handleDeleteOrder = (orderId: number) => {
    dispatch(deleteOrder(orderId))
  }

  return (
    <>
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Forecasting</h2>
          <button onClick={() => setShowForecastModal(true)}>Open Forecasting</button>
        </div>
        {forecastResult && (
          <p>{formatForecastSummary(forecastResult.date, forecastResult.ageYears, forecastResult.count)}</p>
        )}
        {forecastError && <p className='forecast-error'>{forecastError}</p>}
      </section>
      <ForecastingModal
        isOpen={showForecastModal}
        onClose={() => setShowForecastModal(false)}
        onSubmit={handleForecastSubmit}
      />
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
                <tr key={order.id} onClick={() => handleOrderClick(order)} className='clickable-row'>
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
      <OrderActionsModal
        isOpen={selectedOrder !== null}
        order={selectedOrder}
        statuses={statuses}
        tasks={selectedOrder ? tasks[selectedOrder.id] : undefined}
        onClose={handleCloseOrderModal}
        onUpdate={handleUpdateOrder}
        onDelete={handleDeleteOrder}
      />
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
