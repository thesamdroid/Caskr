import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchOrders,
  fetchOutstandingTasks
} from '../features/ordersSlice'
import { fetchStatuses } from '../features/statusSlice'
import { fetchBarrels, forecastBarrels, importBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'
import BarrelImportModal from '../components/BarrelImportModal'
import { formatForecastSummary } from '../utils/forecastSummary'

export default function DashboardPage() {
  const dispatch = useAppDispatch()
  const orders = useAppSelector(state => state.orders.items)
  const statuses = useAppSelector(state => state.statuses.items)
  const tasks = useAppSelector(state => state.orders.outstandingTasks)
  const barrels = useAppSelector(state => state.barrels.items)
  const [showForecastModal, setShowForecastModal] = useState(false)
  const [forecastError, setForecastError] = useState<string | null>(null)
  const [forecastResult, setForecastResult] = useState<
    { date: string; count: number; ageYears: number }
  | null>(null)
  const [showImportModal, setShowImportModal] = useState(false)
  const [requireMashBillId, setRequireMashBillId] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const companyId = 1

  useEffect(() => {
    dispatch(fetchOrders()).then(action => {
      if (fetchOrders.fulfilled.match(action)) {
        action.payload.forEach(order => dispatch(fetchOutstandingTasks(order.id)))
      }
    })
    dispatch(fetchStatuses())
    dispatch(fetchBarrels(companyId))
  }, [dispatch, companyId])

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

  const openImportModal = () => {
    setRequireMashBillId(false)
    setImportError(null)
    setShowImportModal(true)
  }

  const handleImport = async ({ file, batchId, mashBillId }: { file: File; batchId?: number; mashBillId?: number }) => {
    try {
      await dispatch(importBarrels({ companyId, file, batchId, mashBillId })).unwrap()
      setShowImportModal(false)
      setRequireMashBillId(false)
      setImportError(null)
      await dispatch(fetchBarrels(companyId))
    } catch (err) {
      if (err && typeof err === 'object') {
        const errorObject = err as { message?: string; requiresMashBillId?: boolean }
        if (errorObject.requiresMashBillId) {
          setRequireMashBillId(true)
        }
        setImportError(errorObject.message ?? 'Unable to import barrels. Please try again.')
      } else {
        setImportError('Unable to import barrels. Please try again.')
      }
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
      <BarrelImportModal
        isOpen={showImportModal}
        requireMashBillId={requireMashBillId}
        error={importError}
        onClose={() => {
          setShowImportModal(false)
          setRequireMashBillId(false)
          setImportError(null)
        }}
        onSubmit={handleImport}
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
          <div className='section-actions'>
            <button onClick={openImportModal}>Import Barrells</button>
          </div>
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
