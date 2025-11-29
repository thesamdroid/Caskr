import { FormEvent, useEffect, useState } from 'react'
import { useAppDispatch } from '../hooks'
import {
  createTtbGaugeRecord,
  updateTtbGaugeRecord,
  TtbGaugeRecord,
  TtbGaugeType
} from '../features/ttbGaugeRecordsSlice'

interface TtbGaugeRecordModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  gaugeRecord: TtbGaugeRecord | null
  barrelId: number
  barrelSku?: string
}

const gaugeTypeOptions = [
  { value: TtbGaugeType.Fill, label: 'Fill' },
  { value: TtbGaugeType.Storage, label: 'Storage' },
  { value: TtbGaugeType.Removal, label: 'Removal' }
]

export default function TtbGaugeRecordModal({
  isOpen,
  onClose,
  onSuccess,
  gaugeRecord,
  barrelId,
  barrelSku
}: TtbGaugeRecordModalProps) {
  const dispatch = useAppDispatch()

  const [gaugeType, setGaugeType] = useState<TtbGaugeType>(TtbGaugeType.Storage)
  const [proof, setProof] = useState<string>('')
  const [temperature, setTemperature] = useState<string>('60')
  const [wineGallons, setWineGallons] = useState<string>('')
  const [notes, setNotes] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const isEditMode = gaugeRecord !== null

  useEffect(() => {
    if (isOpen) {
      if (gaugeRecord) {
        // Edit mode - populate with gauge record data
        setGaugeType(gaugeRecord.gaugeType)
        setProof(gaugeRecord.proof.toString())
        setTemperature(gaugeRecord.temperature.toString())
        setWineGallons(gaugeRecord.wineGallons.toString())
        setNotes(gaugeRecord.notes || '')
      } else {
        // Create mode - set defaults
        setGaugeType(TtbGaugeType.Storage)
        setProof('')
        setTemperature('60')
        setWineGallons('')
        setNotes('')
      }
      setError(null)
      setIsSubmitting(false)
    }
  }, [isOpen, gaugeRecord])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    // Validation
    const proofNum = parseFloat(proof)
    const tempNum = parseFloat(temperature)
    const wineGallonsNum = parseFloat(wineGallons)

    if (isNaN(proofNum) || proofNum < 0 || proofNum > 200) {
      setError('Proof must be between 0 and 200.')
      return
    }

    if (isNaN(tempNum) || tempNum < -40 || tempNum > 150) {
      setError('Temperature must be between -40 and 150 degrees Fahrenheit.')
      return
    }

    if (isNaN(wineGallonsNum) || wineGallonsNum <= 0) {
      setError('Wine gallons must be greater than 0.')
      return
    }

    setIsSubmitting(true)

    try {
      if (isEditMode && gaugeRecord) {
        // Update existing gauge record
        await dispatch(
          updateTtbGaugeRecord({
            id: gaugeRecord.id,
            request: {
              proof: proofNum,
              temperature: tempNum,
              wineGallons: wineGallonsNum,
              notes: notes.trim() || undefined
            }
          })
        ).unwrap()
      } else {
        // Create new gauge record
        await dispatch(
          createTtbGaugeRecord({
            barrelId,
            gaugeType,
            proof: proofNum,
            temperature: tempNum,
            wineGallons: wineGallonsNum,
            notes: notes.trim() || undefined
          })
        ).unwrap()
      }

      onSuccess()
      onClose()
    } catch (err) {
      console.error('[TtbGaugeRecordModal] Failed to save gauge record', err)
      setError(err instanceof Error ? err.message : 'Failed to save gauge record. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <h2 className="text-2xl font-bold mb-4">
          {isEditMode ? 'Edit Gauge Record' : 'Create Gauge Record'}
        </h2>

        {barrelSku && (
          <p className="text-gray-600 mb-4">
            Barrel SKU: <span className="font-semibold">{barrelSku}</span>
          </p>
        )}

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            {!isEditMode && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Gauge Type <span className="text-red-500">*</span>
                </label>
                <select
                  value={gaugeType}
                  onChange={e => setGaugeType(Number(e.target.value) as TtbGaugeType)}
                  className="w-full border border-gray-300 rounded px-3 py-2"
                  required
                >
                  {gaugeTypeOptions.map(option => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Proof <span className="text-red-500">*</span>
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                max="200"
                value={proof}
                onChange={e => setProof(e.target.value)}
                className="w-full border border-gray-300 rounded px-3 py-2"
                placeholder="e.g., 125.50"
                required
              />
              <p className="text-xs text-gray-500 mt-1">Value between 0 and 200</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Temperature (°F) <span className="text-red-500">*</span>
              </label>
              <input
                type="number"
                step="0.01"
                min="-40"
                max="150"
                value={temperature}
                onChange={e => setTemperature(e.target.value)}
                className="w-full border border-gray-300 rounded px-3 py-2"
                placeholder="e.g., 60.00"
                required
              />
              <p className="text-xs text-gray-500 mt-1">Standard is 60°F</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Wine Gallons <span className="text-red-500">*</span>
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                value={wineGallons}
                onChange={e => setWineGallons(e.target.value)}
                className="w-full border border-gray-300 rounded px-3 py-2"
                placeholder="e.g., 53.00"
                required
              />
              <p className="text-xs text-gray-500 mt-1">Volume in gallons</p>
            </div>
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
            <textarea
              value={notes}
              onChange={e => setNotes(e.target.value)}
              className="w-full border border-gray-300 rounded px-3 py-2"
              rows={3}
              placeholder="Optional notes about this gauge reading..."
            />
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {isSubmitting ? 'Saving...' : isEditMode ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
