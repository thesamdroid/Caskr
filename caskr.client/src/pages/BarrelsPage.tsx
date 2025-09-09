import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrels, forecastBarrels } from '../features/barrelsSlice'

function BarrelsPage() {
  const dispatch = useAppDispatch()
  const barrels = useAppSelector(state => state.barrels.items)
  const forecast = useAppSelector(state => state.barrels.forecast)
  const forecastCount = useAppSelector(state => state.barrels.forecastCount)
  const [showModal, setShowModal] = useState(false)
  const [targetDate, setTargetDate] = useState('')
  const [ageStatement, setAgeStatement] = useState('')
  const companyId = 1

  useEffect(() => {
    dispatch(fetchBarrels(companyId))
  }, [dispatch])

  const handleForecast = (e: React.FormEvent) => {
    e.preventDefault()
    const age = parseInt(ageStatement, 10)
    if (!targetDate || isNaN(age)) return
    dispatch(forecastBarrels({ companyId, targetDate, ageYears: age }))
    setShowModal(false)
  }

  return (
    <>
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Barrels</h2>
          <button onClick={() => setShowModal(true)}>Forecasting</button>
        </div>
        {showModal && (
          <div className='modal'>
            <form onSubmit={handleForecast}>
              <label>
                Date:
                <input type='date' value={targetDate} onChange={e => setTargetDate(e.target.value)} />
              </label>
              <label>
                Age Statement:
                <input
                  value={ageStatement}
                  onChange={e => setAgeStatement(e.target.value)}
                  placeholder='e.g. 5 years'
                />
              </label>
              <button type='submit'>Submit</button>
              <button type='button' onClick={() => setShowModal(false)}>Cancel</button>
            </form>
          </div>
        )}
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
