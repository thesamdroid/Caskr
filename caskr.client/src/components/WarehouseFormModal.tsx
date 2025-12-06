import { useState, useEffect } from 'react'
import { useAppDispatch } from '../hooks'
import {
  createWarehouse,
  updateWarehouse,
  fetchWarehouses,
  Warehouse,
  WarehouseType,
  WarehouseRequest
} from '../features/warehousesSlice'

interface WarehouseFormModalProps {
  isOpen: boolean
  onClose: () => void
  warehouse: Warehouse | null
}

const WAREHOUSE_TYPES: { value: WarehouseType; label: string }[] = [
  { value: 'Rickhouse', label: 'Rickhouse' },
  { value: 'Palletized', label: 'Palletized' },
  { value: 'Tank_Farm', label: 'Tank Farm' },
  { value: 'Outdoor', label: 'Outdoor' }
]

function WarehouseFormModal({ isOpen, onClose, warehouse }: WarehouseFormModalProps) {
  const dispatch = useAppDispatch()
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form state
  const [name, setName] = useState('')
  const [warehouseType, setWarehouseType] = useState<WarehouseType>('Rickhouse')
  const [addressLine1, setAddressLine1] = useState('')
  const [addressLine2, setAddressLine2] = useState('')
  const [city, setCity] = useState('')
  const [state, setState] = useState('')
  const [postalCode, setPostalCode] = useState('')
  const [country, setCountry] = useState('USA')
  const [totalCapacity, setTotalCapacity] = useState('')
  const [lengthFeet, setLengthFeet] = useState('')
  const [widthFeet, setWidthFeet] = useState('')
  const [heightFeet, setHeightFeet] = useState('')
  const [notes, setNotes] = useState('')

  // Initialize form when warehouse changes
  useEffect(() => {
    if (warehouse) {
      setName(warehouse.name)
      setWarehouseType(warehouse.warehouseType as WarehouseType)
      setAddressLine1(warehouse.addressLine1 || '')
      setAddressLine2(warehouse.addressLine2 || '')
      setCity(warehouse.city || '')
      setState(warehouse.state || '')
      setPostalCode(warehouse.postalCode || '')
      setCountry(warehouse.country || 'USA')
      setTotalCapacity(warehouse.totalCapacity.toString())
      setLengthFeet(warehouse.lengthFeet?.toString() || '')
      setWidthFeet(warehouse.widthFeet?.toString() || '')
      setHeightFeet(warehouse.heightFeet?.toString() || '')
      setNotes(warehouse.notes || '')
    } else {
      // Reset form for new warehouse
      setName('')
      setWarehouseType('Rickhouse')
      setAddressLine1('')
      setAddressLine2('')
      setCity('')
      setState('')
      setPostalCode('')
      setCountry('USA')
      setTotalCapacity('')
      setLengthFeet('')
      setWidthFeet('')
      setHeightFeet('')
      setNotes('')
    }
    setError(null)
  }, [warehouse, isOpen])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validation
    if (!name.trim()) {
      setError('Warehouse name is required')
      return
    }

    const capacity = parseInt(totalCapacity, 10)
    if (isNaN(capacity) || capacity < 0) {
      setError('Please enter a valid capacity (positive number)')
      return
    }

    const request: WarehouseRequest = {
      name: name.trim(),
      warehouseType,
      addressLine1: addressLine1.trim() || undefined,
      addressLine2: addressLine2.trim() || undefined,
      city: city.trim() || undefined,
      state: state.trim() || undefined,
      postalCode: postalCode.trim() || undefined,
      country: country.trim() || 'USA',
      totalCapacity: capacity,
      lengthFeet: lengthFeet ? parseFloat(lengthFeet) : undefined,
      widthFeet: widthFeet ? parseFloat(widthFeet) : undefined,
      heightFeet: heightFeet ? parseFloat(heightFeet) : undefined,
      notes: notes.trim() || undefined
    }

    setSaving(true)

    try {
      if (warehouse) {
        await dispatch(updateWarehouse({ id: warehouse.id, warehouse: request })).unwrap()
      } else {
        await dispatch(createWarehouse(request)).unwrap()
      }
      dispatch(fetchWarehouses({ includeInactive: false }))
      onClose()
    } catch (err: unknown) {
      const errorMessage = err && typeof err === 'object' && 'message' in err
        ? (err as { message: string }).message
        : 'Failed to save warehouse'
      setError(errorMessage)
    } finally {
      setSaving(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-large" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{warehouse ? 'Edit Warehouse' : 'Create Warehouse'}</h2>
          <button onClick={onClose} className="modal-close" aria-label="Close">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="form-error">{error}</div>
            )}

            <div className="form-row">
              <div className="form-group form-group-large">
                <label htmlFor="warehouse-name">Warehouse Name *</label>
                <input
                  id="warehouse-name"
                  type="text"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  placeholder="e.g., Rickhouse #1"
                  required
                  autoFocus
                />
              </div>

              <div className="form-group">
                <label htmlFor="warehouse-type">Type</label>
                <select
                  id="warehouse-type"
                  value={warehouseType}
                  onChange={e => setWarehouseType(e.target.value as WarehouseType)}
                >
                  {WAREHOUSE_TYPES.map(type => (
                    <option key={type.value} value={type.value}>
                      {type.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <fieldset className="form-fieldset">
              <legend>Address</legend>
              <div className="form-group">
                <label htmlFor="address-line1">Address Line 1</label>
                <input
                  id="address-line1"
                  type="text"
                  value={addressLine1}
                  onChange={e => setAddressLine1(e.target.value)}
                  placeholder="Street address"
                />
              </div>

              <div className="form-group">
                <label htmlFor="address-line2">Address Line 2</label>
                <input
                  id="address-line2"
                  type="text"
                  value={addressLine2}
                  onChange={e => setAddressLine2(e.target.value)}
                  placeholder="Suite, unit, etc."
                />
              </div>

              <div className="form-row">
                <div className="form-group form-group-large">
                  <label htmlFor="city">City</label>
                  <input
                    id="city"
                    type="text"
                    value={city}
                    onChange={e => setCity(e.target.value)}
                    placeholder="e.g., Bardstown"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="state">State</label>
                  <input
                    id="state"
                    type="text"
                    value={state}
                    onChange={e => setState(e.target.value)}
                    placeholder="e.g., KY"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="postal-code">Postal Code</label>
                  <input
                    id="postal-code"
                    type="text"
                    value={postalCode}
                    onChange={e => setPostalCode(e.target.value)}
                    placeholder="e.g., 40004"
                  />
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="country">Country</label>
                <input
                  id="country"
                  type="text"
                  value={country}
                  onChange={e => setCountry(e.target.value)}
                />
              </div>
            </fieldset>

            <fieldset className="form-fieldset">
              <legend>Capacity & Dimensions</legend>
              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="total-capacity">Total Capacity *</label>
                  <input
                    id="total-capacity"
                    type="number"
                    min="0"
                    value={totalCapacity}
                    onChange={e => setTotalCapacity(e.target.value)}
                    placeholder="Number of barrel positions"
                    required
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="length">Length (ft)</label>
                  <input
                    id="length"
                    type="number"
                    step="0.01"
                    min="0"
                    value={lengthFeet}
                    onChange={e => setLengthFeet(e.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="width">Width (ft)</label>
                  <input
                    id="width"
                    type="number"
                    step="0.01"
                    min="0"
                    value={widthFeet}
                    onChange={e => setWidthFeet(e.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="height">Height (ft)</label>
                  <input
                    id="height"
                    type="number"
                    step="0.01"
                    min="0"
                    value={heightFeet}
                    onChange={e => setHeightFeet(e.target.value)}
                  />
                </div>
              </div>
            </fieldset>

            <div className="form-group">
              <label htmlFor="notes">Notes</label>
              <textarea
                id="notes"
                value={notes}
                onChange={e => setNotes(e.target.value)}
                rows={3}
                placeholder="Additional information about this warehouse..."
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="button-secondary" disabled={saving}>
              Cancel
            </button>
            <button type="submit" className="button-primary" disabled={saving}>
              {saving ? 'Saving...' : warehouse ? 'Update Warehouse' : 'Create Warehouse'}
            </button>
          </div>
        </form>

        <style>{`
          .modal-large {
            max-width: 600px;
          }

          .form-error {
            background: var(--color-error-bg, #fee2e2);
            color: var(--color-error);
            padding: 0.75rem;
            border-radius: 4px;
            margin-bottom: 1rem;
          }

          .form-fieldset {
            border: 1px solid var(--color-border);
            border-radius: 4px;
            padding: 1rem;
            margin-bottom: 1rem;
          }

          .form-fieldset legend {
            padding: 0 0.5rem;
            font-weight: 500;
            color: var(--color-text-secondary);
          }

          .form-row {
            display: flex;
            gap: 1rem;
          }

          .form-group {
            flex: 1;
            margin-bottom: 1rem;
          }

          .form-group-large {
            flex: 2;
          }

          .form-group label {
            display: block;
            margin-bottom: 0.25rem;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
          }

          .form-group input,
          .form-group select,
          .form-group textarea {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
            font-size: 1rem;
          }

          .form-group input:focus,
          .form-group select:focus,
          .form-group textarea:focus {
            outline: none;
            border-color: var(--color-primary);
          }

          .form-group textarea {
            resize: vertical;
          }

          .modal-overlay {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1000;
          }

          .modal {
            background: var(--color-background);
            border-radius: 8px;
            width: 90%;
            max-height: 90vh;
            overflow-y: auto;
          }

          .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            border-bottom: 1px solid var(--color-border);
          }

          .modal-header h2 {
            margin: 0;
            font-size: 1.25rem;
          }

          .modal-close {
            background: none;
            border: none;
            font-size: 1.5rem;
            cursor: pointer;
            color: var(--color-text-secondary);
          }

          .modal-body {
            padding: 1rem;
          }

          .modal-footer {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
            padding: 1rem;
            border-top: 1px solid var(--color-border);
          }

          .button-primary {
            background: var(--color-primary);
            color: white;
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-primary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }

          .button-secondary {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            color: var(--color-text);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-secondary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }
        `}</style>
      </div>
    </div>
  )
}

export default WarehouseFormModal
