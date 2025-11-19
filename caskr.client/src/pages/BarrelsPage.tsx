import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrels, forecastBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'
import { formatForecastSummary } from '../utils/forecastSummary'

function BarrelsPage() {
  const dispatch = useAppDispatch()
  const barrels = useAppSelector(state => state.barrels.items)
  const forecast = useAppSelector(state => state.barrels.forecast)
  const forecastCount = useAppSelector(state => state.barrels.forecastCount)
  const forecastDate = useAppSelector(state => state.barrels.forecastDate)
  const forecastAgeYears = useAppSelector(state => state.barrels.forecastAgeYears)
  const [showModal, setShowModal] = useState(false)
  const companyId = 1

  useEffect(() => {
    dispatch(fetchBarrels(companyId))
  }, [dispatch, companyId])

  const handleForecast = async (targetDate: string, ageYears: number) => {
    await dispatch(forecastBarrels({ companyId, targetDate, ageYears })).unwrap()
    setShowModal(false)
  }

  return (
    <>
      <section className="content-section" aria-labelledby="barrels-title">
        <div className="section-header">
          <div>
            <h1 id="barrels-title" className="section-title">Barrels</h1>
            <p className="section-subtitle">Manage barrel inventory and forecasting</p>
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

        {barrels.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">🛢️</div>
            <h3 className="empty-state-title">No barrels in inventory</h3>
            <p className="empty-state-text">Barrels will appear here when orders are created</p>
          </div>
        ) : (
          <div className="table-container">
            <table className="table" role="table" aria-label="Barrels inventory">
              <thead>
                <tr>
                  <th scope="col">SKU</th>
                  <th scope="col">Order ID</th>
                </tr>
              </thead>
              <tbody>
                {barrels.map(b => (
                  <tr key={b.id}>
                    <td><span className="text-gold">{b.sku}</span></td>
                    <td><span className="text-secondary">{b.orderId}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {forecast.length > 0 && (
        <section className="content-section" aria-labelledby="forecast-title">
          <div className="section-header">
            <div>
              <h2 id="forecast-title" className="section-title">Forecast Results</h2>
              <p className="section-subtitle text-gold">
                {formatForecastSummary(forecastDate, forecastAgeYears, forecastCount)}
              </p>
            </div>
          </div>

          <div className="table-container">
            <table className="table" role="table" aria-label="Forecasted barrels">
              <thead>
                <tr>
                  <th scope="col">SKU</th>
                </tr>
              </thead>
              <tbody>
                {forecast.map(b => (
                  <tr key={b.id}>
                    <td><span className="text-gold">{b.sku}</span></td>
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
