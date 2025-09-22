import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrels, forecastBarrels, importBarrels } from '../features/barrelsSlice'
import ForecastingModal from '../components/ForecastingModal'
import BarrelImportModal from '../components/BarrelImportModal'

function BarrelsPage() {
  const dispatch = useAppDispatch()
  const barrels = useAppSelector(state => state.barrels.items)
  const forecast = useAppSelector(state => state.barrels.forecast)
  const forecastCount = useAppSelector(state => state.barrels.forecastCount)
  const forecastDate = useAppSelector(state => state.barrels.forecastDate)
  const forecastAgeYears = useAppSelector(state => state.barrels.forecastAgeYears)
  const [showModal, setShowModal] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)
  const [requireMashBillId, setRequireMashBillId] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const companyId = 1

  useEffect(() => {
    dispatch(fetchBarrels(companyId))
  }, [dispatch, companyId])

  const handleForecast = async (targetDate: string, ageYears: number) => {
    await dispatch(forecastBarrels({ companyId, targetDate, ageYears })).unwrap()
    setShowModal(false)
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

  return (
    <>
      <section className='content-section'>
        <div className='section-header'>
          <h2 className='section-title'>Barrels</h2>
          <div className='section-actions'>
            <button onClick={() => setShowModal(true)}>Forecasting</button>
            <button onClick={openImportModal}>Import CSV</button>
          </div>
        </div>
        <ForecastingModal
          isOpen={showModal}
          onClose={() => setShowModal(false)}
          onSubmit={handleForecast}
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
            <h2 className='section-title'>Forecast Result</h2>
          </div>
          <p>{formatForecastSummary(forecastDate, forecastAgeYears, forecastCount)}</p>
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
