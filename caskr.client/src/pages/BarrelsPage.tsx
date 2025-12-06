import { useEffect, useState, useMemo } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrels, forecastBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'
import { formatForecastSummary } from '../utils/forecastSummary'

function BarrelsPage() {
  const dispatch = useAppDispatch()
  const barrels = useAppSelector(state => state.barrels.items)
  const forecast = useAppSelector(state => state.barrels.forecast)
  const forecastDate = useAppSelector(state => state.barrels.forecastDate)
  const forecastAgeYears = useAppSelector(state => state.barrels.forecastAgeYears)
  const [showModal, setShowModal] = useState(false)

  // Warehouse filtering
  const selectedWarehouseId = useAppSelector(state => state.warehouses.selectedWarehouseId)
  const warehouses = useAppSelector(state => state.warehouses.items)

  // Get warehouse name by ID
  const getWarehouseName = (warehouseId: number) => {
    const warehouse = warehouses.find(w => w.id === warehouseId)
    return warehouse?.name || '-'
  }

  useEffect(() => {
    dispatch(fetchBarrels())
  }, [dispatch])

  // Filter barrels by selected warehouse
  const filteredBarrels = useMemo(() => {
    if (selectedWarehouseId === null) {
      return barrels
    }
    return barrels.filter(b => b.warehouseId === selectedWarehouseId)
  }, [barrels, selectedWarehouseId])

  // Filter forecast by selected warehouse
  const filteredForecast = useMemo(() => {
    if (selectedWarehouseId === null) {
      return forecast
    }
    return forecast.filter(b => b.warehouseId === selectedWarehouseId)
  }, [forecast, selectedWarehouseId])

  const handleForecast = async (targetDate: string, ageYears: number) => {
    await dispatch(forecastBarrels({ targetDate, ageYears })).unwrap()
    setShowModal(false)
  }

  // Get the warehouse name for display in subtitle
  const selectedWarehouseName = selectedWarehouseId !== null
    ? warehouses.find(w => w.id === selectedWarehouseId)?.name
    : null

  return (
    <>
      <section className="content-section" aria-labelledby="barrels-title">
        <div className="section-header">
          <div>
            <h1 id="barrels-title" className="section-title">Barrels</h1>
            <p className="section-subtitle">
              {selectedWarehouseName
                ? `Showing ${filteredBarrels.length} barrels in ${selectedWarehouseName}`
                : `Manage barrel inventory and forecasting (${filteredBarrels.length} total)`}
            </p>
          </div>
          <div className="section-actions">
            <button
              onClick={() => setShowModal(true)}
              className="button-primary"
              aria-label="Open forecasting modal"
            >
              Run Forecast
            </button>
          </div>
        </div>

        <ForecastingModal
          isOpen={showModal}
          onClose={() => setShowModal(false)}
          onSubmit={handleForecast}
        />

        {filteredBarrels.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🛢️</div>
            <h3 className="empty-state-title">
              {selectedWarehouseName
                ? `No barrels in ${selectedWarehouseName}`
                : 'No barrels in inventory'}
            </h3>
            <p className="empty-state-text">
              {selectedWarehouseName
                ? 'Try selecting a different warehouse or "All Warehouses"'
                : 'Barrels will appear here when orders are created'}
            </p>
          </div>
        ) : (
          <div className="table-container">
            <table className="table" role="table" aria-label="Barrels inventory">
              <thead>
                <tr>
                  <th scope="col">SKU</th>
                  <th scope="col">Warehouse</th>
                  <th scope="col">Order ID</th>
                </tr>
              </thead>
              <tbody>
                {filteredBarrels.map(b => (
                  <tr key={b.id}>
                    <td><span className="text-gold">{b.sku}</span></td>
                    <td><span className="text-secondary">{getWarehouseName(b.warehouseId)}</span></td>
                    <td><span className="text-secondary">{b.orderId}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {filteredForecast.length > 0 && (
        <section className="content-section" aria-labelledby="forecast-title">
          <div className="section-header">
            <div>
              <h2 id="forecast-title" className="section-title">Forecast Results</h2>
              <p className="section-subtitle text-gold">
                {formatForecastSummary(forecastDate, forecastAgeYears, filteredForecast.length)}
                {selectedWarehouseName && ` (in ${selectedWarehouseName})`}
              </p>
            </div>
          </div>

          <div className="table-container">
            <table className="table" role="table" aria-label="Forecasted barrels">
              <thead>
                <tr>
                  <th scope="col">SKU</th>
                  <th scope="col">Warehouse</th>
                </tr>
              </thead>
              <tbody>
                {filteredForecast.map(b => (
                  <tr key={b.id}>
                    <td><span className="text-gold">{b.sku}</span></td>
                    <td><span className="text-secondary">{getWarehouseName(b.warehouseId)}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </>
  )
}

export default BarrelsPage
