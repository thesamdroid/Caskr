import { useEffect, useState } from 'react'
import { useAppSelector } from '../hooks'
import { authorizedFetch } from '../api/authorizedFetch'
import { downloadBlob } from '../utils/downloadBlob'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
  orderName?: string
  companyId?: number
}

const LabelModal = ({ isOpen, onClose, orderName, companyId: fallbackCompanyId }: Props) => {
  const user = useAppSelector(state => state.users.items[0])
  const [brandName, setBrandName] = useState('')
  const [productName, setProductName] = useState('')
  const [alcoholContent, setAlcoholContent] = useState('')
  const [preview, setPreview] = useState<{ blob: Blob; url: string } | null>(null)
  const [isGenerating, setIsGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setProductName(orderName ?? '')
      setError(null)
    } else {
      setBrandName('')
      setProductName('')
      setAlcoholContent('')
      setPreview(null)
      setIsGenerating(false)
      setError(null)
    }
  }, [isOpen, orderName])

  useEffect(() => {
    return () => {
      if (preview) {
        URL.revokeObjectURL(preview.url)
      }
    }
  }, [preview])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (isGenerating) return

    // Validate required fields
    if (!brandName.trim()) {
      setError('Brand name is required')
      return
    }
    if (!productName.trim()) {
      setError('Product name is required')
      return
    }
    if (!alcoholContent.trim()) {
      setError('Alcohol content is required')
      return
    }

    const companyId = user?.companyId ?? fallbackCompanyId
    if (companyId == null) {
      setError('No company ID available. Please try logging in again.')
      return
    }

    setError(null)
    setIsGenerating(true)
    try {
      const response = await authorizedFetch('/api/labels/ttb-form', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/pdf' },
        body: JSON.stringify({
          companyId,
          brandName,
          productName,
          alcoholContent
        })
      })
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to generate label document')
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
    downloadBlob(preview.blob, 'ttb_form_5100_31.pdf')
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
        <h2>Create Label</h2>
        {preview ? (
          <>
            <section className='document-preview' aria-label='Generated TTB document preview' role='region'>
              <iframe src={preview.url} title='TTB label preview' />
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
            <label>
              Brand Name <span style={{ color: 'red' }}>*</span>
              <input
                value={brandName}
                onChange={e => { setBrandName(e.target.value); setError(null); }}
                placeholder='e.g., Kentucky Reserve'
                required
                disabled={isGenerating}
              />
            </label>
            <label>
              Product Name <span style={{ color: 'red' }}>*</span>
              <input
                value={productName}
                onChange={e => { setProductName(e.target.value); setError(null); }}
                onFocus={() => {
                  if (orderName && productName === orderName) {
                    setProductName('')
                  }
                }}
                placeholder='e.g., Straight Bourbon Whiskey'
                required
                disabled={isGenerating}
              />
            </label>
            <label>
              Alcohol Content <span style={{ color: 'red' }}>*</span>
              <input
                value={alcoholContent}
                onChange={e => { setAlcoholContent(e.target.value); setError(null); }}
                placeholder='e.g., 45% ABV'
                required
                disabled={isGenerating}
              />
            </label>
            <div className='modal-actions'>
              <button type='submit' disabled={isGenerating}>
                {isGenerating ? 'Generating...' : 'Generate Label'}
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

export default LabelModal
