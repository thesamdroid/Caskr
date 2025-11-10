import { useEffect, useState } from 'react'
import { useAppSelector } from '../hooks'
import { authorizedFetch } from '../api/authorizedFetch'
import { downloadBlob } from '../utils/downloadBlob'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
  orderId?: number
  orderName?: string
}

const TransferModal = ({ isOpen, onClose, orderId, orderName }: Props) => {
  const user = useAppSelector(state => state.users.items[0])
  const [toCompanyName, setToCompanyName] = useState('')
  const [permitNumber, setPermitNumber] = useState('')
  const [address, setAddress] = useState('')
  const [barrelCount, setBarrelCount] = useState(1)
  const [preview, setPreview] = useState<{ blob: Blob; url: string } | null>(null)
  const [isGenerating, setIsGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setError(null)
    } else {
      setToCompanyName('')
      setPermitNumber('')
      setAddress('')
      setBarrelCount(1)
      setPreview(null)
      setIsGenerating(false)
      setError(null)
    }
  }, [isOpen])

  useEffect(() => {
    return () => {
      if (preview) {
        URL.revokeObjectURL(preview.url)
      }
    }
  }, [preview])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!user || isGenerating) return

    // Validate required fields
    if (!toCompanyName.trim()) {
      setError('Destination company name is required')
      return
    }
    if (barrelCount < 1) {
      setError('Barrel count must be at least 1')
      return
    }

    setError(null)
    setIsGenerating(true)
    try {
      const response = await authorizedFetch('/api/transfers/ttb-form', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/pdf' },
        body: JSON.stringify({
          fromCompanyId: user.companyId,
          toCompanyName,
          permitNumber,
          address,
          barrelCount,
          orderId // Pass orderId to backend for barrel auto-population
        })
      })
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to generate transfer document')
      }
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      setPreview({ blob, url })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred')
    } finally {
      setIsGenerating(false)
    }
  }

  const handleDownload = () => {
    if (!preview) return
    downloadBlob(preview.blob, 'ttb_form_5100_16.pdf')
  }

  const handleBackToForm = () => {
    setPreview(null)
  }

  const handleClose = () => {
    setPreview(null)
    onClose()
  }

  if (!isOpen) return null

  return (
    <div className='modal-overlay'>
      <div className='modal'>
        <h2>Transfer Stock</h2>
        {orderName && <p style={{ marginTop: 0, color: '#666', fontSize: '0.9rem' }}>Order: {orderName}</p>}
        {preview ? (
          <>
            <section className='document-preview' aria-label='Generated TTB document preview' role='region'>
              <iframe src={preview.url} title='TTB transfer preview' />
            </section>
            <div className='modal-actions'>
              <button type='button' onClick={handleDownload}>Download PDF</button>
              <button type='button' onClick={handleBackToForm}>Back to form</button>
              <button type='button' onClick={handleClose}>Close</button>
            </div>
          </>
        ) : (
          <form onSubmit={handleSubmit} aria-busy={isGenerating}>
            {error && <p className='error-message' style={{ color: 'red', marginBottom: '1rem' }}>{error}</p>}
            {orderId && <p style={{ padding: '0.5rem', backgroundColor: '#e3f2fd', borderRadius: '4px', fontSize: '0.9rem' }}>
              ✓ Barrel details will be automatically included from this order
            </p>}
            <label>
              Destination Company <span style={{ color: 'red' }}>*</span>
              <input
                value={toCompanyName}
                onChange={e => { setToCompanyName(e.target.value); setError(null); }}
                placeholder='e.g., ABC Distillery LLC'
                required
                disabled={isGenerating}
              />
            </label>
            <label>
              Permit Number
              <input
                value={permitNumber}
                onChange={e => { setPermitNumber(e.target.value); setError(null); }}
                placeholder='e.g., DSP-KY-12345 (optional)'
                disabled={isGenerating}
              />
            </label>
            <label>
              Destination Address
              <input
                value={address}
                onChange={e => { setAddress(e.target.value); setError(null); }}
                placeholder='e.g., 123 Bourbon St, Louisville, KY (optional)'
                disabled={isGenerating}
              />
            </label>
            <label>
              Number of Barrels <span style={{ color: 'red' }}>*</span>
              <input
                type='number'
                min={1}
                value={barrelCount}
                onChange={e => { setBarrelCount(Number(e.target.value)); setError(null); }}
                placeholder='e.g., 50'
                required
                disabled={isGenerating}
              />
            </label>
            <div className='modal-actions'>
              <button type='submit' disabled={isGenerating}>
                {isGenerating ? 'Generating...' : 'Generate Transfer Form'}
              </button>
              <button type='button' onClick={handleClose} disabled={isGenerating}>
                Cancel
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}

export default TransferModal
