import { useEffect, useState } from 'react'
import { useAppSelector } from '../hooks'
import { authorizedFetch } from '../api/authorizedFetch'
import { downloadBlob } from '../utils/downloadBlob'
import './CreateOrderModal.css'

type Props = {
  isOpen: boolean
  onClose: () => void
}

const TransferModal = ({ isOpen, onClose }: Props) => {
  const user = useAppSelector(state => state.users.items[0])
  const [toCompanyName, setToCompanyName] = useState('')
  const [permitNumber, setPermitNumber] = useState('')
  const [address, setAddress] = useState('')
  const [barrelCount, setBarrelCount] = useState(1)
  const [preview, setPreview] = useState<{ blob: Blob; url: string } | null>(null)
  const [isGenerating, setIsGenerating] = useState(false)

  useEffect(() => {
    if (!isOpen) {
      setToCompanyName('')
      setPermitNumber('')
      setAddress('')
      setBarrelCount(1)
      setPreview(null)
      setIsGenerating(false)
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
          barrelCount
        })
      })
      if (!response.ok) {
        throw new Error('Failed to generate transfer document')
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
            <input value={toCompanyName} onChange={e => setToCompanyName(e.target.value)} placeholder='Destination Company' />
            <input value={permitNumber} onChange={e => setPermitNumber(e.target.value)} placeholder='Permit Number' />
            <input value={address} onChange={e => setAddress(e.target.value)} placeholder='Destination Address' />
            <input type='number' min={1} value={barrelCount} onChange={e => setBarrelCount(Number(e.target.value))} />
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

export default TransferModal
