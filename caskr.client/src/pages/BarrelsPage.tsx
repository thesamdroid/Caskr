import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrels, forecastBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'

function BarrelsPage() {
  const dispatch = useAppDispatch()
  const barrels = useAppSelector(state => state.barrels.items)
  const forecast = useAppSelector(state => state.barrels.forecast)
  const forecastCount = useAppSelector(state => state.barrels.forecastCount)
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
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Barrels</h2>
          <button onClick={() => setShowModal(true)}>Forecasting</button>
        </div>
        <ForecastingModal
          isOpen={showModal}
          onClose={() => setShowModal(false)}
          onSubmit={handleForecast}
        />
        <div className='table-container'>
          <table className='table'>
            <thead>
              <tr>
                <th>SKU</th>
                <th>Order</th>
              </tr>
            </thead>
            <tbody>
              {barrels.map(b => (
                <tr key={b.id}>
                  <td>{b.sku}</td>
                  <td>{b.orderId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      {forecast.length > 0 && (
        <section className='content-section'>
          <div className='section-header'>
            <h2 className='section-title'>Forecast Result (Total: {forecastCount})</h2>
          </div>
          <div className='table-container'>
            <table className='table'>
              <thead>
                <tr>
                  <th>SKU</th>
                </tr>
              </thead>
              <tbody>
                {forecast.map(b => (
                  <tr key={b.id}>
                    <td>{b.sku}</td>
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
