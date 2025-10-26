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

  useEffect(() => {
    if (isOpen) {
      setProductName(orderName ?? '')
    } else {
      setBrandName('')
      setProductName('')
      setAlcoholContent('')
      setPreview(null)
      setIsGenerating(false)
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
    const companyId = user?.companyId ?? fallbackCompanyId
    if (companyId == null) {
      console.error('No company ID available for label generation')
      return
    }
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
        throw new Error('Failed to generate label document')
      }
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      setPreview({ blob, url })
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
            <input value={brandName} onChange={e => setBrandName(e.target.value)} placeholder='Brand Name' />
            <input value={productName} onChange={e => setProductName(e.target.value)} placeholder='Product Name' />
            <input value={alcoholContent} onChange={e => setAlcoholContent(e.target.value)} placeholder='Alcohol Content' />
            <div className='modal-actions'>
              <button type='submit' disabled={isGenerating}>
                Generate
              </button>
              <button type='button' onClick={handleClose}>
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
