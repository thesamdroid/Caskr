import { FormEvent, useEffect, useMemo, useState } from 'react'

interface TtbReportGenerationModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (month: number, year: number) => Promise<void>
  defaultYear: number
  isSubmitting: boolean
  errorMessage?: string | null
}

const monthOptions = Array.from({ length: 12 }, (_, index) => ({
  value: index + 1,
  label: new Intl.DateTimeFormat('en-US', { month: 'long' }).format(new Date(2024, index, 1))
}))

export default function TtbReportGenerationModal({
  isOpen,
  onClose,
  onSubmit,
  defaultYear,
  isSubmitting,
  errorMessage
}: TtbReportGenerationModalProps) {
  const [selectedMonth, setSelectedMonth] = useState<number>(new Date().getMonth() + 1)
  const [selectedYear, setSelectedYear] = useState<number>(defaultYear)
  const [localError, setLocalError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setSelectedMonth(new Date().getMonth() + 1)
      setSelectedYear(defaultYear)
      setLocalError(null)
    }
  }, [defaultYear, isOpen])

  const yearOptions = useMemo(() => {
    return Array.from({ length: 6 }, (_, index) => defaultYear - index)
  }, [defaultYear])

  if (!isOpen) return null

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setLocalError(null)

    if (!selectedMonth || !selectedYear) {
      setLocalError('Please select a month and year to generate the report.')
      return
    }

    try {
      await onSubmit(selectedMonth, selectedYear)
    } catch (error) {
      console.error('[TtbReportGenerationModal] Failed to generate report', {
        month: selectedMonth,
        year: selectedYear,
        error
      })
      setLocalError('We were unable to generate this report. Please try again.')
    }
  }

  return (
    <div className='modal' role='dialog' aria-modal='true' aria-labelledby='generate-ttb-report-heading'>
      <form className='modal-content' onSubmit={handleSubmit}>
        <div className='modal-header'>
          <h3 id='generate-ttb-report-heading'>Generate TTB Report</h3>
          <p className='modal-subtitle'>Select the reporting period to create a draft Form 5110.28 PDF.</p>
        </div>

        <div className='modal-grid'>
          <label className='form-label' htmlFor='report-month'>
            Reporting month
            <select
              id='report-month'
              value={selectedMonth}
              onChange={event => setSelectedMonth(Number(event.target.value))}
              required
              aria-required='true'
            >
              {monthOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className='form-label' htmlFor='report-year'>
            Reporting year
            <select
              id='report-year'
              value={selectedYear}
              onChange={event => setSelectedYear(Number(event.target.value))}
              required
              aria-required='true'
            >
              {yearOptions.map(option => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
        </div>

        {(localError || errorMessage) && (
          <div className='alert alert-error' role='alert' aria-live='assertive'>
            {localError ?? errorMessage}
          </div>
        )}

        <div className='modal-actions'>
          <button type='submit' className='button-primary' disabled={isSubmitting}>
            {isSubmitting ? 'Generating…' : 'Generate report'}
          </button>
          <button type='button' onClick={onClose} className='button-secondary'>
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
