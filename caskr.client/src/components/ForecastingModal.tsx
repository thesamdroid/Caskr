import { FormEvent, useEffect, useState } from 'react'

interface ForecastingModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (targetDate: string, ageYears: number) => void | Promise<void>
}

export default function ForecastingModal({ isOpen, onClose, onSubmit }: ForecastingModalProps) {
  const [targetDate, setTargetDate] = useState('')
  const [ageYears, setAgeYears] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setTargetDate('')
      setAgeYears('')
      setError(null)
    }
  }, [isOpen])

  if (!isOpen) return null

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!targetDate) {
      setError('Please choose a date to forecast.')
      return
    }

    const parsedAge = ageYears.trim() === '' ? 0 : parseInt(ageYears, 10)
    if (isNaN(parsedAge) || parsedAge < 0) {
      setError('Age must be a positive number or left blank.')
      return
    }

    console.log('[ForecastingModal] Submitting forecast request', { targetDate, ageYears: parsedAge })
    try {
      await onSubmit(targetDate, parsedAge)
      console.log('[ForecastingModal] Forecast submission completed successfully', {
        targetDate,
        ageYears: parsedAge
      })
    } catch (e) {
      console.error('[ForecastingModal] Forecast submission failed', { targetDate, ageYears: parsedAge, error: e })
      setError('Unable to forecast barrels. Please try again.')
    }
  }

  return (
    <div className='modal'>
      <form onSubmit={handleSubmit} className='modal-content'>
        <h3>Forecast Barrels</h3>
        <label>
          Date
          <input type='date' value={targetDate} onChange={event => setTargetDate(event.target.value)} />
        </label>
        <label>
          Age Statement (years)
          <input
            type='number'
            min={0}
            placeholder='Optional'
            value={ageYears}
            onChange={event => setAgeYears(event.target.value)}
          />
        </label>
        {error && <p className='forecast-error'>{error}</p>}
        <div className='modal-actions'>
          <button type='submit'>Submit</button>
          <button type='button' onClick={onClose}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
